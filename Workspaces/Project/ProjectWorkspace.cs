using UnityEngine;
using UnityEngine.VR.Utilities;

public class ProjectWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ContentPrefab;

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		var projectUI = contentPrefab.GetComponent<ProjectUI>();
#if UNITY_EDITOR
		projectUI.listView.data = AssetData.GetAssetDataForPath(Application.dataPath);
#else
		Debug.LogWarning("Project workspace does not work in builds");
#endif
	}

	protected override void OnBoundsChanged()
	{
		throw new System.NotImplementedException();
	}
}