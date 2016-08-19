using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	private static readonly float kInitReferenceYOffset = kDefaultBounds.y / 3; //1/3 of initial initial Y bounds
	private const float kInitReferenceScale = 25; //We want to see a big region by default

	//Scale slider min/max (maps to referenceTransform unifrom scale)
	private const float kMinScale = 0.1f;
	private const float kMaxScale = 35;

	[SerializeField]
	private GameObject m_ContentPrefab;

	[SerializeField]
	private GameObject m_UIPrefab;

	private ChessboardUI m_ChessboardUI;
	private MiniWorld m_MiniWorld;
	private Material m_GridMaterial;

	private readonly List<RayData> m_RayData = new List<RayData>(2);
	private float m_ScaleStartDistance;

	private class RayData
	{
		public Transform rayOrigin;
		public Vector3 rayOriginStart;
		public Vector3 refTransformStartPosition;
		public Vector3 refTransformStartScale;
	}

	public override void Setup()
	{
		base.Setup();
		U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ChessboardUI = GetComponentInChildren<ChessboardUI>();
		m_GridMaterial = U.Material.GetMaterialClone(m_ChessboardUI.grid);

		//Set up MiniWorld
		m_MiniWorld = GetComponentInChildren<MiniWorld>();
		m_MiniWorld.referenceTransform.position = Vector3.up * kInitReferenceYOffset;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * kInitReferenceScale;

		//Set up ControlBox
		//ControlBox shouldn't move with miniWorld
		var panZoomHandle = m_ChessboardUI.panZoomHandle;
		panZoomHandle.transform.parent = m_WorkspaceUI.sceneContainer;
		panZoomHandle.transform.localPosition = Vector3.down * panZoomHandle.transform.localScale.y * 0.5f;
		panZoomHandle.onHandleBeginDrag += OnControlBeginDrag;
		panZoomHandle.onHandleDrag += OnControlDrag;
		panZoomHandle.onHandleEndDrag += OnControlEndDrag;
		panZoomHandle.onHoverEnter += OnControlHoverEnter;
		panZoomHandle.onHoverExit += OnControlHoverExit;

		//Set up UI
		var UI = U.Object.InstantiateAndSetActive(m_UIPrefab, m_WorkspaceUI.frontPanel, false);
		var zoomSliderUI = UI.GetComponentInChildren<ZoomSliderUI>();
		zoomSliderUI.sliding = Sliding;
		zoomSliderUI.zoomSlider.maxValue = kMaxScale;
		zoomSliderUI.zoomSlider.minValue = kMinScale;
		zoomSliderUI.zoomSlider.value = kInitReferenceScale;
		OnBoundsChanged();
	}

	private void Update()
	{
		//Set grid height, deactivate if out of bounds
		float gridHeight = m_MiniWorld.referenceTransform.position.y / m_MiniWorld.referenceTransform.localScale.y;
		var grid = m_ChessboardUI.grid;
		if (Mathf.Abs(gridHeight) < contentBounds.extents.y)
		{
			grid.gameObject.SetActive(true);
			grid.transform.localPosition = Vector3.down * gridHeight;
		}
		else
		{
			grid.gameObject.SetActive(false);
		}

		//Update grid material if ClipBox has moved
		m_GridMaterial.mainTextureScale = new Vector2(
			m_MiniWorld.referenceTransform.localScale.x * contentBounds.size.x,
			m_MiniWorld.referenceTransform.localScale.z * contentBounds.size.z);
		m_GridMaterial.mainTextureOffset =
			Vector2.one * 0.5f //Center grid
			+ new Vector2(m_GridMaterial.mainTextureScale.x % 2, m_GridMaterial.mainTextureScale.y % 2) * -0.5f //Scaling offset
			+ new Vector2(m_MiniWorld.referenceTransform.position.x, m_MiniWorld.referenceTransform.position.z); //Translation offset
	}

	protected override void OnBoundsChanged()
	{
		m_MiniWorld.transform.localPosition = Vector3.up * contentBounds.extents.y;
		m_MiniWorld.localBounds = contentBounds;

		m_ChessboardUI.grid.transform.localScale = new Vector3(contentBounds.size.x, contentBounds.size.z, 1);

		var controlBox = m_ChessboardUI.panZoomHandle;
		controlBox.transform.localScale = new Vector3(contentBounds.size.x, controlBox.transform.localScale.y, contentBounds.size.z);
	}

	private void Sliding(float value)
	{
		m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
	}

	private void OnControlBeginDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_RayData.Count == 1) //On introduction of second ray
		{
			m_ScaleStartDistance = (m_RayData[0].rayOrigin.position - eventData.rayOrigin.position).magnitude;
		}

		m_RayData.Add(new RayData
		{
			rayOrigin = eventData.rayOrigin,
			rayOriginStart = eventData.rayOrigin.position,
			refTransformStartPosition = m_MiniWorld.referenceTransform.position,
			refTransformStartScale = m_MiniWorld.referenceTransform.localScale
		});
	}

	private void OnControlDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		//if (m_RayData.Count == 0)
		//	return;
		var rayData = m_RayData[0];
		if (!eventData.rayOrigin.Equals(rayData.rayOrigin)) //We only want one event per frame
			return;
		var referenceTransform = m_MiniWorld.referenceTransform;
		var rayOrigin = eventData.rayOrigin;
		//Translate
		referenceTransform.position = rayData.refTransformStartPosition
									+ Vector3.Scale(rayData.rayOriginStart - rayOrigin.transform.position, referenceTransform.localScale);
		//If we have two rays, also scale
		if (m_RayData.Count > 1)
		{
			var otherRay = m_RayData[1];
			referenceTransform.localScale = otherRay.refTransformStartScale * (m_ScaleStartDistance
										/ (otherRay.rayOrigin.position - rayOrigin.position).magnitude);
		}
	}

	private void OnControlEndDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_RayData.RemoveAll(rayData => rayData.rayOrigin.Equals(eventData.rayOrigin));
	}

	private void OnControlHoverEnter(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		setHighlight(handle.gameObject, true);
	}

	private void OnControlHoverExit(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		setHighlight(handle.gameObject, false);
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_GridMaterial);
	}
}