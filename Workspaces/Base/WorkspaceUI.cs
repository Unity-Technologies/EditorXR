using System;
using UnityEngine;

public class WorkspaceUI : MonoBehaviour
{
	public Action OnHandleClick { private get; set; }
	public Action OnCloseClick { private get; set; }
	public GameObject sceneContainer;
	public RectTransform handle;
	[SerializeField]
	private Transform m_BoundsCube;

	public void SetBounds(Bounds bounds)
	{
		handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x + Workspace.handleMargin);
		handle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.size.z + Workspace.handleMargin);
		m_BoundsCube.transform.localScale = bounds.size;
		m_BoundsCube.transform.localPosition = Vector3.up * bounds.extents.y;
	}
	public void HandleClick()
	{
		OnHandleClick();
	}

	public void CloseClick()
	{
		OnCloseClick();
	}
}
