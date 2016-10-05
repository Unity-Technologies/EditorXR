using UnityEngine;
using UnityEngine.VR.Workspaces;
using UnityEditor.VR;

public class ProfilerWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ProfilerWindowPrefab;

	private Transform m_ProfilerWindow;

	private bool inView { get; set; }

	private RectTransform m_CaptureWindowRect;

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

		UnityEditorInternal.ProfilerDriver.profileEditor = false;
		inView = false;

		m_CaptureWindowRect = GetComponentInChildren<EditorWindowCapture>().GetComponent<RectTransform>();
	}

	void Update()
	{
		UpdateProfilerInView();
		UnityEditorInternal.ProfilerDriver.profileEditor = inView;
	}

	void UpdateProfilerInView()
	{
		Vector3[] corners = new Vector3[4];
		m_CaptureWindowRect.GetWorldCorners(corners);

		//use a smaller rect than the full viewerCamera to re-enable only when enough of the profiler is in view.
		float minX = VRView.viewerCamera.pixelRect.width * .25f;
		float minY = VRView.viewerCamera.pixelRect.height * .25f;
		float maxX = VRView.viewerCamera.pixelRect.width * .75f;
		float maxY = VRView.viewerCamera.pixelRect.height * .75f;

		inView = false;
		foreach(Vector3 vec in corners)
		{
			Vector3 screenPoint = VRView.viewerCamera.WorldToScreenPoint(vec);
			if(screenPoint.x > minX && screenPoint.x < maxX && screenPoint.y > minY && screenPoint.y < maxY)
			{
				inView = true;
				break;
			}
		}
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