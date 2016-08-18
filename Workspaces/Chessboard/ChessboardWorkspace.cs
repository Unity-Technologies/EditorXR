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

	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
	[SerializeField]
	private GameObject m_ContentPrefab;

	[SerializeField]
	private GameObject m_UIPrefab;

	private ChessboardSceneObjects m_ChessboardSceneObjects;
	private MiniWorld m_MiniWorld;
	private Material m_GridMaterial;

	private readonly RayData[] m_RayData = new RayData[2];
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
		U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceSceneObjects.sceneContainer, false);
		m_ChessboardSceneObjects = GetComponentInChildren<ChessboardSceneObjects>();
		m_GridMaterial = m_ChessboardSceneObjects.grid.sharedMaterial;

		//Set up MiniWorld
		m_MiniWorld = GetComponentInChildren<MiniWorld>();
		m_MiniWorld.referenceTransform.position = Vector3.up * kInitReferenceYOffset;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * kInitReferenceScale;

		//Set up ControlBox
		//ControlBox shouldn't move with miniWorld
		var controlBox = m_ChessboardSceneObjects.controlBox;
		controlBox.transform.parent = m_WorkspaceSceneObjects.sceneContainer;
		controlBox.transform.localPosition = Vector3.down * controlBox.transform.localScale.y * 0.5f;
		controlBox.onHandleBeginDrag += ControlDragStart;
		controlBox.onHandleDrag += ControlDrag;
		controlBox.onHandleEndDrag += ControlDragEnd;
		controlBox.onHoverEnter += ControlHoverEnter;
		controlBox.onHoverExit += ControlHoverExit;

		//Set up UI
		var UI = U.Object.InstantiateAndSetActive(m_UIPrefab, m_WorkspaceSceneObjects.frontPanel, false);
		var chessboardUI = UI.GetComponentInChildren<ChessboardUI>();
		chessboardUI.OnZoomSlider = OnZoomSlider;
		chessboardUI.zoomSlider.maxValue = kMaxScale;
		chessboardUI.zoomSlider.minValue = kMinScale;
		chessboardUI.zoomSlider.value = kInitReferenceScale;
		OnBoundsChanged();
	}

	private void Update()
	{
		//Set grid height, deactivate if out of bounds
		float gridHeight = m_MiniWorld.referenceTransform.position.y / m_MiniWorld.referenceTransform.localScale.y;
		var grid = m_ChessboardSceneObjects.grid;
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
		m_MiniWorld.SetBounds(contentBounds);

		m_ChessboardSceneObjects.grid.transform.localScale = new Vector3(contentBounds.size.x, contentBounds.size.z, 1);

		var controlBox = m_ChessboardSceneObjects.controlBox;
		controlBox.transform.localScale = new Vector3(contentBounds.size.x, controlBox.transform.localScale.y, contentBounds.size.z);
	}

	private void OnZoomSlider(float value)
	{
		m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
	}

	private void ControlDragStart(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_RayData[0] != null && m_RayData[1] == null) //On introduction of second ray
		{
			m_ScaleStartDistance = (m_RayData[0].rayOrigin.position - eventData.rayOrigin.position).magnitude;
		}
		for (var i = 0; i < m_RayData.Length; i++)
		{
			if (m_RayData[i] != null) continue;

			m_RayData[i] = new RayData
			{
				rayOrigin = eventData.rayOrigin,
				rayOriginStart = eventData.rayOrigin.position,
				refTransformStartPosition = m_MiniWorld.referenceTransform.position,
				refTransformStartScale = m_MiniWorld.referenceTransform.localScale
			};
			break;
		}
	}

	private void ControlDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		var rayData = m_RayData[0];
		if (rayData != null)
		{
			var refTransform = m_MiniWorld.referenceTransform;
			var rayOrigin = eventData.rayOrigin;
			if (m_RayData[1] == null)	//Translate
			{
				refTransform.position = rayData.refTransformStartPosition 
										+ Vector3.Scale(rayData.rayOriginStart - rayOrigin.transform.position, refTransform.localScale);
			}
			//If we have two rays set and this is the event for the first one
			else if (m_RayData[0].rayOrigin.Equals(rayOrigin)) //Translate/Scale
			{
				refTransform.position = rayData.refTransformStartPosition 
										+ Vector3.Scale(rayData.rayOriginStart - rayOrigin.transform.position, refTransform.localScale);

				var otherRay = m_RayData[1];
				refTransform.localScale = otherRay.refTransformStartScale * (m_ScaleStartDistance 
										/ (otherRay.rayOrigin.position - rayOrigin.position).magnitude);
			}
		}
	}

	private void ControlDragEnd(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		for(var i = 0; i < m_RayData.Length; i++)
			if (m_RayData[i] != null && m_RayData[i].rayOrigin.Equals(eventData.rayOrigin))
				m_RayData[i] = null;
	}

	private void ControlHoverEnter(BaseHandle handle)
	{
		setHighlight(handle.gameObject, true);
	}

	private void ControlHoverExit(BaseHandle handle)
	{
		setHighlight(handle.gameObject, false);
	}
}