using UnityEngine;
using UnityEngine.VR.Tools;

public class ConsoleWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ConsoleWindowPrefab;

	private Transform m_ConsoleWindow;

	public override void Setup()
	{
		base.Setup();

		m_ConsoleWindow = instantiateUI(m_ConsoleWindowPrefab).transform;
		m_ConsoleWindow.SetParent(m_WorkspaceUI.sceneContainer, false);

		var bounds = contentBounds;
		var size = bounds.size;
		size.z = 0.1f;
		bounds.size = size;
		contentBounds = bounds;

		m_WorkspaceUI.boundsVisible = false;

		m_ConsoleWindow.localScale = size;
	}

	protected override void OnBoundsChanged()
	{
		m_ConsoleWindow.localScale = contentBounds.size;
	}
}