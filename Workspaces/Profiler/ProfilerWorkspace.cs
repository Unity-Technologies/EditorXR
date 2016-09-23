using UnityEngine;
using UnityEngine.VR.Workspaces;
using UnityEditor.VR;

public class ProfilerWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ProfilerWindowPrefab;

	private Transform m_ProfilerWindow;

	public override void Setup()
	{
		base.Setup();

		m_ProfilerWindow = instantiateUI(m_ProfilerWindowPrefab).transform;
		m_ProfilerWindow.SetParent(m_WorkspaceUI.sceneContainer,false);

		var bounds = contentBounds;
		var size = bounds.size;
		size.z = 0.1f;
		bounds.size = size;
		contentBounds = bounds;

		m_ProfilerWindow.localScale = size;

		UnityEditorInternal.ProfilerDriver.profileEditor = true;
	}

	void Update()
	{
		UpdateProfilerInView();
	}

	void UpdateProfilerInView()
	{
		RaycastHit hit;
		Ray ray = VRView.viewerCamera.ViewportPointToRay(new Vector2(0.5f,0.5f));
		if(Physics.Raycast(ray,out hit))
		{
			if(hit.collider.GetComponentInParent<ProfilerWorkspace>() != null)
			{
				UnityEditorInternal.ProfilerDriver.profileEditor = true;
				return;
			}
		}

		UnityEditorInternal.ProfilerDriver.profileEditor = false;
	}

	protected override void OnBoundsChanged()
	{
		m_ProfilerWindow.localScale = contentBounds.size;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		UnityEditorInternal.ProfilerDriver.profileEditor = false;
	}
}