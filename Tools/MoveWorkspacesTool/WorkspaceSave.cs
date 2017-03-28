using UnityEngine;

[System.Serializable]
public class WorkspaceSave
{
	public WorkspaceSaveData[] workspaces;
	public WorkspaceSave(int numberOfWorkspaces)
	{
		workspaces = new WorkspaceSaveData[numberOfWorkspaces];
	}
}

[System.Serializable]
public struct WorkspaceSaveData
{
	public string workspaceName;
	public Vector3 localPosition;
	public Quaternion localRotation;
	public Bounds bounds;
}
