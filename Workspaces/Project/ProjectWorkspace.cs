using UnityEngine;
using UnityEngine.VR.Utilities;

public class ProjectWorkspace : Workspace
{
	private const float kLeftPaneRatio = 0.33333f; //Size of left pane relative to workspace bounds

	[SerializeField]
	private GameObject m_ContentPrefab;

	private ProjectUI m_ProjectUI;

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();
#if UNITY_EDITOR
		m_ProjectUI.listView.data = AssetData.GetAssetDataForPath(Application.dataPath);
#else
		Debug.LogWarning("Project workspace does not work in builds");
#endif

		//Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.x *= kLeftPaneRatio;
		size.y = 0;
		bounds.size = size;
		bounds.center = Vector3.zero;
		m_ProjectUI.listView.bounds = bounds;
		m_ProjectUI.listView.transform.localPosition = contentBounds.size.x * kLeftPaneRatio * Vector3.left;
	}
}