using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class ChessboardWorkspace : Workspace
{
	private const float kClipBoxYOffset = 0.1333333f;			//1/3 of initial initial Y bounds (0.4)
	private const float kClipBoxInitScale = 25;					//We want to see a big region by default
	private const float kMinScale = 0.1f;
	private const float kMaxScale = 35;

	//NOTE: since pretty much all workspaces will want a prefab, should this go in the base class?
	[SerializeField]
	private GameObject m_ContentPrefab;
	[SerializeField]
	private GameObject m_UIPrefab;

	private MiniWorld m_MiniWorld;
	private ChessboardPrefab m_ChessboardPrefab;
	private Material m_GridMaterial;

	private readonly ControlData[] m_ControlDatas = new ControlData[2];
	private float m_ScaleStartDistance;

	private class ControlData
	{
		public Transform rayOrigin;
		public Vector3 rayOriginStart;
		public Vector3 referenceTransformStartPosition;
		public Vector3 referenceTransformStartScale;
	}

	public override void Setup()
	{
		base.Setup();
		U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspacePrefab.sceneContainer, false);
		//Set up MiniWorld
		m_MiniWorld = GetComponentInChildren<MiniWorld>();
		m_MiniWorld.referenceTransform.position = Vector3.up * kClipBoxYOffset;
		m_MiniWorld.referenceTransform.localScale = Vector3.one * kClipBoxInitScale;
		m_ChessboardPrefab = GetComponentInChildren<ChessboardPrefab>();
		m_GridMaterial = m_ChessboardPrefab.grid.sharedMaterial;

		//Set up ControlBox
		//ControlBox shouldn't move with miniWorld
		var controlBox = m_ChessboardPrefab.controlBox;
		controlBox.transform.parent = m_WorkspacePrefab.sceneContainer;
		controlBox.transform.localPosition = Vector3.down * controlBox.transform.localScale.y * 0.5f;
		controlBox.onHandleBeginDrag += ControlDragStart;
		controlBox.onHandleDrag += ControlDrag;
		controlBox.onHandleEndDrag += ControlDragEnd;
		controlBox.onHoverEnter += ControlHoverEnter;
		controlBox.onHoverExit += ControlHoverExit;

		//Set up UI
		var UI = U.Object.InstantiateAndSetActive(m_UIPrefab, m_WorkspacePrefab.frontPanel, false);
		var chessboardUI = UI.GetComponentInChildren<ChessboardUI>();
		chessboardUI.OnZoomSlider = OnZoomSlider;
		chessboardUI.zoomSlider.maxValue = kMaxScale;
		chessboardUI.zoomSlider.minValue = kMinScale;
		chessboardUI.zoomSlider.value = kClipBoxInitScale;
		OnBoundsChanged();
	}

	private void Update()
	{
		//Set grid height, deactivate if out of bounds
		float gridHeight = m_MiniWorld.referenceTransform.position.y / m_MiniWorld.referenceTransform.localScale.y;
		if (Mathf.Abs(gridHeight) < contentBounds.extents.y)
		{
			m_ChessboardPrefab.grid.gameObject.SetActive(true);
			m_ChessboardPrefab.grid.transform.localPosition = Vector3.down * gridHeight;
		}
		else
		{
			m_ChessboardPrefab.grid.gameObject.SetActive(false);
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

		m_ChessboardPrefab.grid.transform.localScale = new Vector3(contentBounds.size.x, contentBounds.size.z, 1);

		var controlBox = m_ChessboardPrefab.controlBox;
		controlBox.transform.localScale = new Vector3(contentBounds.size.x, controlBox.transform.localScale.y, contentBounds.size.z);
	}

	private void OnZoomSlider(float value)
	{
		m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
	}

	private void ControlDragStart(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		if (m_ControlDatas[0] != null && m_ControlDatas[1] == null)
		{
			m_ScaleStartDistance = (m_ControlDatas[0].rayOrigin.position - eventData.rayOrigin.position).magnitude;
		}
		for (var i = 0; i < m_ControlDatas.Length; i++)
		{
			if (m_ControlDatas[i] == null)
			{
				m_ControlDatas[i] = new ControlData
				{
					rayOrigin = eventData.rayOrigin,
					rayOriginStart = eventData.rayOrigin.position,
					referenceTransformStartPosition = m_MiniWorld.referenceTransform.position,
					referenceTransformStartScale = m_MiniWorld.referenceTransform.localScale
				};
				break;
			}
		}
	}

	private void ControlDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		var controlData = m_ControlDatas[0];
		if (controlData != null)
		{
			if (m_ControlDatas[1] == null)	//Translate
			{
				m_MiniWorld.referenceTransform.position = controlData.referenceTransformStartPosition + Vector3.Scale(controlData.rayOriginStart - eventData.rayOrigin.transform.position, m_MiniWorld.referenceTransform.localScale);
			}
			//If we have two controllers set and this is the event for the first one
			else if (m_ControlDatas[0].rayOrigin.Equals(eventData.rayOrigin)) //Translate/Scale
			{
				m_MiniWorld.referenceTransform.position = controlData.referenceTransformStartPosition + Vector3.Scale(controlData.rayOriginStart - eventData.rayOrigin.transform.position, m_MiniWorld.referenceTransform.localScale);

				ControlData otherControl = m_ControlDatas[1];
				m_MiniWorld.referenceTransform.localScale = otherControl.referenceTransformStartScale * (m_ScaleStartDistance / (otherControl.rayOrigin.position - eventData.rayOrigin.position).magnitude);
			}
		}
	}

	private void ControlDragEnd(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		for(var i = 0; i < m_ControlDatas.Length; i++)
			if (m_ControlDatas[i] != null && m_ControlDatas[i].rayOrigin.Equals(eventData.rayOrigin))
				m_ControlDatas[i] = null;
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
