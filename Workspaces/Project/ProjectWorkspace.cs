using UnityEngine;
using UnityEngine.VR.Utilities;

public class ProjectWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ContentPrefab;

	public override void Setup()
	{
		base.Setup();
		U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
	}

	protected override void OnBoundsChanged()
	{
		throw new System.NotImplementedException();
	}
}