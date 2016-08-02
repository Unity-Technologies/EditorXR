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

	public void OnHandleClick()
	{
		Debug.Log("click");
	}

	public void Close()
	{
		owner.Close();
	}
}
