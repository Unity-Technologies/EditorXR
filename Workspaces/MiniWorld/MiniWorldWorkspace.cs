using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;
using Button = UnityEngine.UI.Button;
using UnityEngine.InputNew;

[MainMenuItem("MiniWorld", "Workspaces", "Edit a smaller version of your scene(s)")]
public class MiniWorldWorkspace : Workspace, IUsesRayLocking, ICustomActionMap
{
	private static readonly float kInitReferenceYOffset = kDefaultBounds.y / 2.001f; // Show more space above ground than below
	private const float kInitReferenceScale = 10f; // We want to see a big region by default

	//TODO: replace with dynamic values once spatial hash lands
	// Scale slider min/max (maps to referenceTransform uniform scale)
	private const float kMinZoomScale = 0.5f;
	private const float kMaxZoomScale = 200f;

	[SerializeField]
	private GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_RecenterUIPrefab;

	[SerializeField]
	private GameObject m_LocatePlayerPrefab;

	[SerializeField]
	private GameObject m_PlayerDirectionArrowPrefab;

	[SerializeField]
	private GameObject m_ZoomSliderPrefab;

	public ActionMap actionMap { get { return m_MiniWorldActionMap; } }
	[SerializeField]
	ActionMap m_MiniWorldActionMap;

	private MiniWorldUI m_MiniWorldUI;
	private MiniWorld m_MiniWorld;
	private Material m_GridMaterial;
	private ZoomSliderUI m_ZoomSliderUI;
	private Transform m_PlayerDirectionButton;
	private Transform m_PlayerDirectionArrow;
	readonly List<Transform> m_Rays = new List<Transform>(2);
	float m_StartScale;
	float m_StartDistance;
	Vector3 m_StartPosition;
	Vector3 m_StartOffset;
	Vector3 m_StartMidPoint;
	Vector3 m_StartDirection;
	float m_StartYaw;

	Coroutine m_UpdateLocationCoroutine;

	public Func<Transform, object, bool> lockRay { get; set; }
	public Func<Transform, object, bool> unlockRay { get; set; }

	public IMiniWorld miniWorld { get { return m_MiniWorld; } }

	public Transform leftRayOrigin { get; set; }
	public Transform rightRayOrigin { get; set; }

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

		// Set up Zoom Slider
		var sliderObject = U.Object.Instantiate(m_ZoomSliderPrefab, m_WorkspaceUI.frontPanel, false);
		m_ZoomSliderUI = sliderObject.GetComponentInChildren<ZoomSliderUI>();
		m_ZoomSliderUI.sliding += OnSliding;
		m_ZoomSliderUI.zoomSlider.maxValue = Mathf.Log10(kMaxZoomScale);
		m_ZoomSliderUI.zoomSlider.minValue = Mathf.Log10(kMinZoomScale);
		m_ZoomSliderUI.zoomSlider.direction = Slider.Direction.RightToLeft; // Invert direction for expected ux; zoom in as slider moves left to right
		m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(kInitReferenceScale);
		foreach (var mb in m_ZoomSliderUI.GetComponentsInChildren<MonoBehaviour>())
		{
			connectInterfaces(mb);
		}

		var zoomTooltip = sliderObject.GetComponentInChildren<Tooltip>();
		if (zoomTooltip)
			zoomTooltip.tooltipText = "Drag the Handle to Zoom the Mini World";

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

	public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
	{
		var miniWorldInput = (MiniWorldInput)input;

		if (miniWorld.Contains(leftRayOrigin.position) && miniWorldInput.leftGrab.wasJustPressed)
		{
			OnPanZoomDragStarted(leftRayOrigin);
			consumeControl(miniWorldInput.leftGrab);
		}

		if (miniWorld.Contains(rightRayOrigin.position) && miniWorldInput.rightGrab.wasJustPressed)
		{
			OnPanZoomDragStarted(rightRayOrigin);
			consumeControl(miniWorldInput.rightGrab);
		}

		if (miniWorldInput.leftGrab.isHeld || miniWorldInput.rightGrab.isHeld)
			OnPanZoomDragging();

		if (miniWorldInput.leftGrab.wasJustReleased)
			OnPanZoomDragEnded(leftRayOrigin);

		if (miniWorldInput.rightGrab.wasJustReleased)
			OnPanZoomDragEnded(rightRayOrigin);
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

	}

	private void OnSliding(float value)
	{
		ScaleMiniWorld(Mathf.Pow(10, value));
	}

	void ScaleMiniWorld(float value)
	{
		var scaleDiff = (value - m_MiniWorld.referenceTransform.localScale.x) / m_MiniWorld.referenceTransform.localScale.x;
		m_MiniWorld.referenceTransform.position += Vector3.up * m_MiniWorld.referenceBounds.extents.y * scaleDiff;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
	}

	void OnPanZoomDragStarted(Transform rayOrigin)
	{
		var referenceTransform = miniWorld.referenceTransform;
		m_StartPosition = referenceTransform.position;
		var rayOriginPosition = miniWorld.miniWorldTransform.InverseTransformPoint(rayOrigin.position);

		// On introduction of second ray
		if (m_Rays.Count == 1)
		{
			var rayToRay = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[0].position) - rayOriginPosition;
			var midPoint = rayOriginPosition + rayToRay * 0.5f;
			m_StartScale = referenceTransform.localScale.x;
			m_StartDistance = rayToRay.magnitude;
			m_StartMidPoint = U.Math.ConstrainYawRotation(referenceTransform.rotation) * midPoint;
			m_StartDirection = rayToRay;
			m_StartDirection.y = 0;
			m_StartOffset = m_StartMidPoint * m_StartScale;

			m_StartPosition += m_StartOffset;
			m_StartYaw = referenceTransform.rotation.eulerAngles.y;
		}
		else
		{
			m_StartMidPoint = rayOriginPosition;
		}

		m_Rays.Add(rayOrigin);
	}

	void OnPanZoomDragging()
	{
		var rayCount = m_Rays.Count;
		if (rayCount == 0)
			return;

		var firstRayPosition = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[0].position);
		var referenceTransform = m_MiniWorld.referenceTransform;

		// If we have two rays, scale
		if (rayCount > 1)
		{
			var secondRayPosition = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[1].position);

			var rayToRay = firstRayPosition - secondRayPosition;
			var midPoint = secondRayPosition + rayToRay * 0.5f;

			var scaleFactor = m_StartDistance / rayToRay.magnitude;
			var currentScale = m_StartScale * scaleFactor;

			m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(referenceTransform.localScale.x);

			rayToRay.y = 0;
			var yawSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90, Vector3.down) * m_StartDirection, rayToRay));
			var rotationDiff = Vector3.Angle(m_StartDirection, rayToRay) * yawSign;
			var currentRotation = Quaternion.AngleAxis(m_StartYaw + rotationDiff, Vector3.up);
			var worldMidPoint = currentRotation * midPoint;
			referenceTransform.rotation = currentRotation;
			referenceTransform.localScale = Vector3.one * currentScale;

			referenceTransform.position = m_StartPosition - m_StartOffset * scaleFactor
				+ (m_StartMidPoint - worldMidPoint) * currentScale;
		}
		else
		{
			referenceTransform.position = m_StartPosition + referenceTransform.rotation
				* Vector3.Scale(m_StartMidPoint - firstRayPosition, referenceTransform.localScale);
		}
	}

	void OnPanZoomDragEnded(Transform rayOrigin)
	{
		m_Rays.RemoveAll(rayData => rayData.Equals(rayOrigin));

		// Set up remaining ray with new offset
		if (m_Rays.Count > 0)
		{
			var firstRay = m_Rays[0];
			var referenceTransform = m_MiniWorld.referenceTransform;
			m_StartMidPoint = miniWorld.miniWorldTransform.InverseTransformPoint(firstRay.position);
			m_StartPosition = referenceTransform.position;
			m_StartScale = referenceTransform.localScale.x;
		}
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
		m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(m_MiniWorld.referenceTransform.localScale.x);

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