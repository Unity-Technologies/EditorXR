using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VR.Handles;

public class WorkspaceUI : MonoBehaviour
{
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

	public BaseHandle vacuumHandle { get { return m_VacuumHandle; } }
	[SerializeField]
	private BaseHandle m_VacuumHandle;

	public LinearHandle leftHandle { get { return m_LeftHandle; } }
	[SerializeField]
	private LinearHandle m_LeftHandle;

	public LinearHandle frontHandle { get { return m_FrontHandle; } }
	[SerializeField]
	private LinearHandle m_FrontHandle;

	public LinearHandle rightHandle { get { return m_RightHandle; } }
	[SerializeField]
	private LinearHandle m_RightHandle;

	public LinearHandle backHandle { get { return m_BackHandle; } }
	[SerializeField]
	private LinearHandle m_BackHandle;

	[FormerlySerializedAs("m_Tray")]
	[SerializeField]
	private SkinnedMeshRenderer m_Frame;

	[SerializeField]
	private Transform m_BoundsCube;

	public Action OnCloseClick { private get; set; }
	public Action OnLockClick { private get; set; }

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
	}

	public void CloseClick()
	{
		OnCloseClick();
	}

	public void LockClick() {
		OnLockClick();
	}
}