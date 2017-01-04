using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

[MainMenuItem("MiniWorld", "Workspaces", "Edit a smaller version of your scene(s)")]
public class MiniWorldWorkspace : Workspace, IRayLocking
{
	private static readonly float kInitReferenceYOffset = kDefaultBounds.y / 2.001f; // Show more space above ground than below
	private const float kInitReferenceScale = 15f; // We want to see a big region by default

	//TODO: replace with dynamic values once spatial hash lands
	// Scale slider min/max (maps to referenceTransform unifrom scale)
	private const float kMinScale = 0.1f;
	private const float kMaxScale = 35;

	[SerializeField]
	private GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_RecenterUIPrefab;

	[SerializeField]
	private GameObject m_LocatePlayerPrefab;

	[SerializeField]
	private GameObject m_PlayerDirectionArrowPrefab;

	[SerializeField]
	private GameObject m_UIPrefab;

	private MiniWorldUI m_MiniWorldUI;
	private MiniWorld m_MiniWorld;
	private Material m_GridMaterial;
	private ZoomSliderUI m_ZoomSliderUI;
	private Transform m_PlayerDirectionButton;
	private Transform m_PlayerDirectionArrow;
	private readonly List<RayData> m_RayData = new List<RayData>(2);
	private float m_ScaleStartDistance;
	bool m_PanZooming;
	Coroutine m_UpdateLocationCoroutine;

	private class RayData
	{
		public Transform rayOrigin;
		public Vector3 rayOriginStart;
		public Vector3 refTransformStartPosition;
		public Vector3 refTransformStartScale;
	}

	public Func<Transform, object, bool> lockRay { get; set; }
	public Func<Transform, object, bool> unlockRay { get; set; }

	public IMiniWorld miniWorld { get { return m_MiniWorld; } }

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.25f);
		m_CustomStartingBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.5f);

		base.Setup();

		U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_MiniWorldUI = GetComponentInChildren<MiniWorldUI>();
		m_GridMaterial = U.Material.GetMaterialClone(m_MiniWorldUI.grid);

		var resetUI = U.Object.Instantiate(m_RecenterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponentInChildren<ResetUI>();
		resetUI.resetButton.onClick.AddListener(ResetChessboard);
		foreach (var mb in resetUI.GetComponentsInChildren<MonoBehaviour>())
		{
			connectInterfaces(mb);
		}
		
		var parent = m_WorkspaceUI.frontPanel.parent;
		var locatePlayerUI = U.Object.Instantiate(m_LocatePlayerPrefab, parent, false);
		m_PlayerDirectionButton = locatePlayerUI.transform.GetChild(0);
		foreach (var mb in locatePlayerUI.GetComponentsInChildren<MonoBehaviour>())
		{
			var button = mb as Button;
			if (button)
				button.onClick.AddListener(RecenterOnPlayer);
		}

		var arrow = U.Object.Instantiate(m_PlayerDirectionArrowPrefab, parent, false);
		arrow.transform.localPosition = new Vector3(-0.232f, 0.03149995f, 0f);
		m_PlayerDirectionArrow = arrow.transform;
		
		// Set up MiniWorld
		m_MiniWorld = GetComponentInChildren<MiniWorld>();
		m_MiniWorld.referenceTransform.position = Vector3.up * kInitReferenceYOffset * kInitReferenceScale;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * kInitReferenceScale;

		// Set up ControlBox
		var panZoomHandle = m_MiniWorldUI.panZoomHandle;
		// ControlBox shouldn't move with miniWorld
		panZoomHandle.transform.parent = m_WorkspaceUI.sceneContainer;
		panZoomHandle.transform.localPosition = Vector3.down * panZoomHandle.transform.localScale.y * 0.5f;
		panZoomHandle.dragStarted += OnPanZoomDragStarted;
		panZoomHandle.dragging += OnPanZoomDragging;
		panZoomHandle.dragEnded += OnPanZoomDragEnded;
		panZoomHandle.hoverStarted += OnPanZoomHoverStarted;
		panZoomHandle.hoverEnded += OnPanZoomHoverEnded;

		// Set up UI
		var UI = U.Object.Instantiate(m_UIPrefab, m_WorkspaceUI.frontPanel, false);
		m_ZoomSliderUI = UI.GetComponentInChildren<ZoomSliderUI>();
		m_ZoomSliderUI.sliding += OnSliding;
		m_ZoomSliderUI.zoomSlider.maxValue = kMaxScale;
		m_ZoomSliderUI.zoomSlider.minValue = kMinScale;
		m_ZoomSliderUI.zoomSlider.direction = Slider.Direction.RightToLeft; // Invert direction for expected ux; zoom in as slider moves left to right
		m_ZoomSliderUI.zoomSlider.value = kInitReferenceScale;
		foreach (var mb in m_ZoomSliderUI.GetComponentsInChildren<MonoBehaviour>())
			connectInterfaces(mb);

		var frontHandle = m_WorkspaceUI.directManipulator.GetComponent<BaseHandle>();
		frontHandle.dragStarted += DragStarted;
		frontHandle.dragEnded += DragEnded;

		// Propagate initial bounds
		OnBoundsChanged();
	}

	private void Update()
	{
		var inBounds = IsPlayerInBounds();
		m_PlayerDirectionButton.gameObject.SetActive(!inBounds);
		m_PlayerDirectionArrow.gameObject.SetActive(!inBounds);

		if (!inBounds)
			UpdatePlayerDirectionArrow();

		//Set grid height, deactivate if out of bounds
		float gridHeight = m_MiniWorld.referenceTransform.position.y / m_MiniWorld.referenceTransform.localScale.y;
		var grid = m_MiniWorldUI.grid;
		if (Mathf.Abs(gridHeight) < contentBounds.extents.y)
		{
			grid.gameObject.SetActive(true);
			grid.transform.localPosition = Vector3.down * gridHeight;
		}
		else
		{
			grid.gameObject.SetActive(false);
		}

		// Update grid material if ClipBox has moved
		m_GridMaterial.mainTextureScale = new Vector2(
			m_MiniWorld.referenceTransform.localScale.x * contentBounds.size.x,
			m_MiniWorld.referenceTransform.localScale.z * contentBounds.size.z);
		m_GridMaterial.mainTextureOffset =
			Vector2.one * 0.5f // Center grid
			+ new Vector2(m_GridMaterial.mainTextureScale.x % 2, m_GridMaterial.mainTextureScale.y % 2) * -0.5f // Scaling offset
			+ new Vector2(m_MiniWorld.referenceTransform.position.x, m_MiniWorld.referenceTransform.position.z); // Translation offset
	}

	protected override void OnBoundsChanged()
	{
		m_MiniWorld.transform.localPosition = Vector3.up * contentBounds.extents.y;
		const float kOffsetToAccountForFrameSize = -0.14f;
		// NOTE: We are correcting bounds because the mesh needs to be updated
		var correctedBounds = new Bounds(contentBounds.center, new Vector3(contentBounds.size.x, contentBounds.size.y, contentBounds.size.z + kOffsetToAccountForFrameSize));
		m_MiniWorld.localBounds = correctedBounds;

		m_MiniWorldUI.boundsCube.transform.localScale = correctedBounds.size;

		m_MiniWorldUI.grid.transform.localScale = new Vector3(correctedBounds.size.x, correctedBounds.size.z, 1);

		var controlBox = m_MiniWorldUI.panZoomHandle;
		controlBox.transform.localScale = new Vector3(correctedBounds.size.x, controlBox.transform.localScale.y, correctedBounds.size.z);
	}

	private void OnSliding(float value)
	{
		ScaleMiniWorld(value);
	}

	void ScaleMiniWorld(float value)
	{
		var scaleDiff = (value - m_MiniWorld.referenceTransform.localScale.x) / m_MiniWorld.referenceTransform.localScale.x;
		m_MiniWorld.referenceTransform.position += Vector3.up * m_MiniWorld.referenceBounds.extents.y * scaleDiff;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
	}

	void OnPanZoomDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_PanZooming = true;
		m_WorkspaceUI.topHighlight.visible = true;

		if (m_RayData.Count == 1) // On introduction of second ray
			m_ScaleStartDistance = (m_RayData[0].rayOrigin.position - eventData.rayOrigin.position).magnitude;

		m_RayData.Add(new RayData
		{
			rayOrigin = eventData.rayOrigin,
			rayOriginStart = eventData.rayOrigin.position,
			refTransformStartPosition = m_MiniWorld.referenceTransform.position,
			refTransformStartScale = m_MiniWorld.referenceTransform.localScale
		});
	}

	void OnPanZoomDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		var rayData = m_RayData[0];
		if (!eventData.rayOrigin.Equals(rayData.rayOrigin)) // Do not execute for the second ray
			return;

		var referenceTransform = m_MiniWorld.referenceTransform;
		var rayOrigin = eventData.rayOrigin;
		
		// Rotate translation by inverse workspace yaw
		Quaternion yawRotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.down);

		// Translate
		referenceTransform.position = rayData.refTransformStartPosition
			+ yawRotation * Vector3.Scale(rayData.rayOriginStart - rayOrigin.transform.position, referenceTransform.localScale);

		// If we have two rays, scale
		if (m_RayData.Count > 1)
		{
			var otherRay = m_RayData[1];
			referenceTransform.localScale = otherRay.refTransformStartScale * (m_ScaleStartDistance
				/ (otherRay.rayOrigin.position - rayOrigin.position).magnitude);

			m_ZoomSliderUI.zoomSlider.value = referenceTransform.localScale.x;
		}
	}

	void OnPanZoomDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_PanZooming = false;
		m_WorkspaceUI.topHighlight.visible = false;

		m_RayData.RemoveAll(rayData => rayData.rayOrigin.Equals(eventData.rayOrigin));
	}

	void OnPanZoomHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_WorkspaceUI.topHighlight.visible = true;
	}

	void OnPanZoomHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_PanZooming)
			m_WorkspaceUI.topHighlight.visible = false;
	}

	void DragStarted(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		lockRay(handleEventData.rayOrigin, this);
	}

	void DragEnded(BaseHandle baseHandle, HandleEventData handleEventData)
	{
		unlockRay(handleEventData.rayOrigin, this);
	}

	void RecenterOnPlayer()
	{
		this.RestartCoroutine(ref m_UpdateLocationCoroutine, UpdateLocation(U.Camera.GetMainCamera().transform.position));
	}

	void ResetChessboard()
	{
		ScaleMiniWorld(kInitReferenceScale);
		m_ZoomSliderUI.zoomSlider.value = m_MiniWorld.referenceTransform.localScale.x;

		this.RestartCoroutine(ref m_UpdateLocationCoroutine, UpdateLocation(Vector3.up * kInitReferenceYOffset * kInitReferenceScale));
	}

	IEnumerator UpdateLocation(Vector3 targetPosition)
	{
		const float kTargetDuration = 0.25f;
		var transform = m_MiniWorld.referenceTransform;
		var smoothVelocity = Vector3.zero;
		var currentDuration = 0f;
		while (currentDuration < kTargetDuration)
		{
			currentDuration += Time.unscaledDeltaTime;
			transform.position = U.Math.SmoothDamp(transform.position, targetPosition, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
			yield return null;
		}

		transform.position = targetPosition;
	}

	bool IsPlayerInBounds()
	{
		return m_MiniWorld.referenceBounds.Contains(U.Camera.GetMainCamera().transform.position);
	}

	void UpdatePlayerDirectionArrow()
	{
		var directionArrowTransform = m_PlayerDirectionArrow.transform;
		var playerPos = U.Camera.GetMainCamera().transform.position;
		var miniWorldPos = m_MiniWorld.referenceTransform.position;
		var targetDir = playerPos - miniWorldPos;
		var newDir = Vector3.RotateTowards(directionArrowTransform.up, targetDir, 360f, 360f);

		directionArrowTransform.localRotation = Quaternion.LookRotation(newDir);
		directionArrowTransform.Rotate(Vector3.right, -90.0f);
	}

	protected override void OnDestroy()
	{
		U.Object.Destroy(m_GridMaterial);
		base.OnDestroy();
	}
}