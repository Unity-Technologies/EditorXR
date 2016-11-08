using UnityEngine;

[System.Serializable]
public class WorkspaceSave
{
	public WorkspaceSaveData[] m_Workspaces;
	public WorkspaceSave(int numberOfWorkspaces)
	{
		m_Workspaces = new WorkspaceSaveData[numberOfWorkspaces];
	}
}

[System.Serializable]
public struct WorkspaceSaveData
{
	public string workspaceName;
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;
}
