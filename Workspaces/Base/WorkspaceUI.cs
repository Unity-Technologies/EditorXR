using System;
using UnityEngine;
public class WorkspaceUI : MonoBehaviour
{
	public Action OnHandleClick { private get; set; }
	public Action OnCloseClick { private get; set; }
	public GameObject sceneContainer;
	public SkinnedMeshRenderer tray;
	public RectTransform frontPanel;

	private const float kPanelOffset = 0.1f; //The panel needs to be pulled back slightly
	[SerializeField]
	private Transform m_BoundsCube;

	public void SetBounds(Bounds bounds)
	{
		//Because BlendShapes cap at 100, our workspace maxes out at 100m wide
		tray.SetBlendShapeWeight(0, bounds.size.x + Workspace.kHandleMargin);
		tray.SetBlendShapeWeight(1, bounds.size.z + Workspace.kHandleMargin);
		m_BoundsCube.transform.localScale = bounds.size;
		m_BoundsCube.transform.localPosition = Vector3.up * bounds.extents.y;
		frontPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x + Workspace.kHandleMargin);
		frontPanel.localPosition = new Vector3(0, frontPanel.localPosition.y, -bounds.extents.z - Workspace.kHandleMargin + kPanelOffset);
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
