using UnityEngine;
using System.Collections;

//NOTE: Need a better name for this class. WorkspaceBase seems like the base class for workspaces.
//This is a MonoBehaviour class mean to coincide with every workspace, providing base functionality.
//It needs to be distinct from the Workspace class so that it can be added to the base prefab and connected to UI events.
public class WorkspaceHandle : MonoBehaviour
{
	//Q: Is this "link up the chain" OK?
	public Workspace owner;
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
	public void OnHandleClick()
	{
		Debug.Log("click");
	}

	public void Close()
	{
		owner.Close();
	}
}
