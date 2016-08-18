using System;
using UnityEngine;
public class WorkspacePrefab : MonoBehaviour
{
	public Action OnCloseClick { private get; set; }
	public Action<Transform, Transform> OnHandleDragStart { private get; set; }
	public Action<Transform, Workspace.Direction> OnHandleDrag { private get; set; }
	public Transform sceneContainer;
	public SkinnedMeshRenderer tray;
	public RectTransform frontPanel;
	public DirectManipulator translateManipulator;
	public Transform leftHandle, frontHandle, rightHandle, backHandle;

	private const float kPanelOffset = 0.1f; //The panel needs to be pulled back slightly

	[SerializeField]
	private Transform m_BoundsCube;

	public void SetBounds(Bounds bounds)
	{
		//Because BlendShapes cap at 100, our workspace maxes out at 100m wide
		tray.SetBlendShapeWeight(0, bounds.size.x + Workspace.kHandleMargin);
		tray.SetBlendShapeWeight(1, bounds.size.z + Workspace.kHandleMargin);

		//Resize handles
		float handleScale = leftHandle.localScale.x;
		leftHandle.localPosition = new Vector3(-bounds.extents.x - Workspace.kHandleMargin + handleScale, leftHandle.localPosition.y, 0);
		leftHandle.localScale = new Vector3(handleScale, handleScale, bounds.size.z + Workspace.kHandleMargin);
		frontHandle.localPosition = new Vector3(0, frontHandle.localPosition.y, -bounds.extents.z - Workspace.kHandleMargin + handleScale);
		frontHandle.localScale = new Vector3(bounds.size.x + Workspace.kHandleMargin, handleScale, handleScale);
		rightHandle.localPosition = new Vector3(bounds.extents.x + Workspace.kHandleMargin - handleScale, rightHandle.localPosition.y, 0);
		rightHandle.localScale = new Vector3(handleScale, handleScale, bounds.size.z + Workspace.kHandleMargin);
		backHandle.localPosition = new Vector3(0, backHandle.localPosition.y, bounds.extents.z + Workspace.kHandleMargin - handleScale);
		backHandle.localScale = new Vector3(bounds.size.x + Workspace.kHandleMargin, handleScale, handleScale);

		//Resize bounds cube
		m_BoundsCube.transform.localScale = bounds.size;
		m_BoundsCube.transform.localPosition = Vector3.up * bounds.extents.y;
		
		//Resize front panel
		frontPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x + Workspace.kHandleMargin);
		frontPanel.localPosition = new Vector3(0, frontPanel.localPosition.y, -bounds.extents.z - Workspace.kHandleMargin + kPanelOffset);
	}

	public void HandleDragStart(Transform handle, Transform rayOrigin)
	{
		OnHandleDragStart(handle, rayOrigin);
	}
	public void HandleDrag(Transform handle, Transform rayOrigin)
	{
		Workspace.Direction direction = Workspace.Direction.LEFT;
		if(handle == frontHandle)
			direction = Workspace.Direction.FRONT;
		if (handle == rightHandle)
			direction = Workspace.Direction.RIGHT;
		if (handle == backHandle)
			direction = Workspace.Direction.BACK;
		OnHandleDrag(rayOrigin, direction);
	}

	public void ControlDragStart(Transform controlBox, Transform rayOrigin)
	{
		
	}

	public void ControlDrag(Transform controlBox, Transform rayOrigin)
	{

	}

	public void CloseClick()
	{
		OnCloseClick();
	}
}
