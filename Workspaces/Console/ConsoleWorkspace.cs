using UnityEngine;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Workspaces;

[MainMenuItem("Console", "Workspaces", "View errors, warnings and other messages")]
public class ConsoleWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ConsoleWindowPrefab;

	private Transform m_ConsoleWindow;

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(0.6f, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		preventFrontBackResize = true;
		preventLeftRightResize = true;
		dynamicFaceAdjustment = false;

		m_ConsoleWindow = instantiateUI(m_ConsoleWindowPrefab).transform;
		m_ConsoleWindow.SetParent(m_WorkspaceUI.topFaceContainer, false);
		m_ConsoleWindow.localPosition = new Vector3(0f, -0.007f, -0.5f);
		m_ConsoleWindow.localRotation = Quaternion.Euler(90f, 0f, 0f);
		m_ConsoleWindow.localScale = new Vector3(1f, 1f, 1f);

		var bounds = contentBounds;
		var size = bounds.size;
		size.z = 0.1f;
		bounds.size = size;
		contentBounds = bounds;
	}
}