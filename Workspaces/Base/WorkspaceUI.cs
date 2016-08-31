using System;
using UnityEngine;
using UnityEngine.VR.Handles;

public class WorkspaceUI : MonoBehaviour
{
	public event Action closeClicked = delegate { };
	public event Action lockClicked = delegate { };

	private const float kPanelOffset = 0f; //The panel needs to be pulled back slightly

	public Transform sceneContainer { get { return m_SceneContainer; } }
	[SerializeField]
	private Transform m_SceneContainer;

	public RectTransform frontPanel { get { return m_FrontPanel; } }
	[SerializeField]
	private RectTransform m_FrontPanel;

	public DirectManipulator directManipulator { get { return m_DirectManipulator; } }
	[SerializeField]
	private DirectManipulator m_DirectManipulator;

	[SerializeField]
	private BoxCollider m_GrabCollider;

	public BaseHandle vacuumHandle { get { return m_VacuumHandle; } }
	[SerializeField]
	private BaseHandle m_VacuumHandle;

	public BaseHandle leftHandle { get { return m_LeftHandle; } }
	[SerializeField]
	private BaseHandle m_LeftHandle;

	public BaseHandle frontHandle { get { return m_FrontHandle; } }
	[SerializeField]
	private BaseHandle m_FrontHandle;

	public BaseHandle rightHandle { get { return m_RightHandle; } }
	[SerializeField]
	private BaseHandle m_RightHandle;

	public BaseHandle backHandle { get { return m_BackHandle; } }
	[SerializeField]
	private BaseHandle m_BackHandle;

	[SerializeField]
	private SkinnedMeshRenderer m_Frame;

	[SerializeField]
	private Transform m_BoundsCube;

	public bool boundsVisible { get { return m_BoundsCube.gameObject.activeInHierarchy; } set { m_BoundsCube.gameObject.SetActive(value); } }

	public void SetBounds(Bounds bounds)
	{
		//Because BlendShapes cap at 100, our workspace maxes out at 100m wide
		m_Frame.SetBlendShapeWeight(0, bounds.size.x + Workspace.kHandleMargin);
		m_Frame.SetBlendShapeWeight(1, bounds.size.z + Workspace.kHandleMargin);

		//Resize handles
		float handleScale = leftHandle.transform.localScale.z;

		m_LeftHandle.transform.localPosition = new Vector3(-bounds.extents.x + handleScale * 0.5f, m_LeftHandle.transform.localPosition.y, 0);
		m_LeftHandle.transform.localScale = new Vector3(bounds.size.z, handleScale, handleScale);

		m_FrontHandle.transform.localPosition = new Vector3(0, m_FrontHandle.transform.localPosition.y, -bounds.extents.z - handleScale);
		m_FrontHandle.transform.localScale = new Vector3(bounds.size.x, handleScale, handleScale);

		m_RightHandle.transform.localPosition = new Vector3(bounds.extents.x - handleScale * 0.5f, m_RightHandle.transform.localPosition.y, 0);
		m_RightHandle.transform.localScale = new Vector3(bounds.size.z, handleScale, handleScale);

		m_BackHandle.transform.localPosition = new Vector3(0, m_BackHandle.transform.localPosition.y, bounds.extents.z - handleScale);
		m_BackHandle.transform.localScale = new Vector3(bounds.size.x, handleScale, handleScale);

		//Resize bounds cube
		m_BoundsCube.transform.localScale = bounds.size;
		m_BoundsCube.transform.localPosition = Vector3.up * bounds.extents.y;

		//Resize front panel
		m_FrontPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x);
		m_FrontPanel.localPosition = new Vector3(0, m_FrontPanel.localPosition.y, -bounds.extents.z + kPanelOffset);

		m_GrabCollider.size = new Vector3(bounds.size.x, m_GrabCollider.size.y, m_GrabCollider.size.z);
	}

	public void CloseClick()
	{
		closeClicked();
	}

	public void LockClick() {
		lockClicked();
	}
}