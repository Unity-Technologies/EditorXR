using System;
using UnityEngine;
using UnityEngine.VR.Handles;

public class WorkspacePrefab : MonoBehaviour
{
	public Transform sceneContainer;
	public RectTransform frontPanel;
	public DirectHandle translateHandle;
	public LinearHandle leftHandle;
	public LinearHandle frontHandle;
	public LinearHandle rightHandle;
	public LinearHandle backHandle;

	private const float kPanelOffset = 0.1f; //The panel needs to be pulled back slightly

	[SerializeField]
	private SkinnedMeshRenderer m_Tray;

	[SerializeField]
	private Transform m_BoundsCube;

	public Action OnCloseClick { private get; set; }

	public void SetBounds(Bounds bounds)
	{
		//Because BlendShapes cap at 100, our workspace maxes out at 100m wide
		m_Tray.SetBlendShapeWeight(0, bounds.size.x + Workspace.kHandleMargin);
		m_Tray.SetBlendShapeWeight(1, bounds.size.z + Workspace.kHandleMargin);

		//Resize handles
		float handleScale = leftHandle.transform.localScale.z;

		leftHandle.transform.localPosition = new Vector3(-bounds.extents.x - Workspace.kHandleMargin + handleScale, leftHandle.transform.localPosition.y, 0);
		leftHandle.transform.localScale = new Vector3(bounds.size.z + Workspace.kHandleMargin, handleScale, handleScale);

		frontHandle.transform.localPosition = new Vector3(0, frontHandle.transform.localPosition.y, -bounds.extents.z - Workspace.kHandleMargin + handleScale);
		frontHandle.transform.localScale = new Vector3(bounds.size.x + Workspace.kHandleMargin, handleScale, handleScale);

		rightHandle.transform.localPosition = new Vector3(bounds.extents.x + Workspace.kHandleMargin - handleScale, rightHandle.transform.localPosition.y, 0);
		rightHandle.transform.localScale = new Vector3(bounds.size.z + Workspace.kHandleMargin, handleScale, handleScale);

		backHandle.transform.localPosition = new Vector3(0, backHandle.transform.localPosition.y, bounds.extents.z + Workspace.kHandleMargin - handleScale);
		backHandle.transform.localScale = new Vector3(bounds.size.x + Workspace.kHandleMargin, handleScale, handleScale);

		//Resize bounds cube
		m_BoundsCube.transform.localScale = bounds.size;
		m_BoundsCube.transform.localPosition = Vector3.up * bounds.extents.y;

		//Resize front panel
		frontPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x + Workspace.kHandleMargin);
		frontPanel.localPosition = new Vector3(0, frontPanel.localPosition.y, -bounds.extents.z - Workspace.kHandleMargin + kPanelOffset);
	}

	public void CloseClick()
	{
		OnCloseClick();
	}
}