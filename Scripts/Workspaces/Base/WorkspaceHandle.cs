using UnityEngine;
using System.Collections;

public class WorkspaceHandle : MonoBehaviour
{
	//Q: Is this "link up the chain" OK?
	public Workspace owner;
	public GameObject contents;

	public void OnHandleClick()
	{
		Debug.Log("click");
	}

	public void Close()
	{
		owner.Close();
	}
}
