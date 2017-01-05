using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Workspaces;

[MainMenuItem("Profiler", "Workspaces", "Analyze your project's performance")]
public class ProfilerWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ProfilerWindowPrefab;

	private Transform m_ProfilerWindow;

#if UNITY_EDITORVR
	bool inView
	{
		get
		{
			Vector3[] corners = new Vector3[4];
			m_CaptureWindowRect.GetWorldCorners(corners);

			//use a smaller rect than the full viewerCamera to re-enable only when enough of the profiler is in view.
			float minX = VRView.viewerCamera.pixelRect.width * .25f;
			float minY = VRView.viewerCamera.pixelRect.height * .25f;
			float maxX = VRView.viewerCamera.pixelRect.width * .75f;
			float maxY = VRView.viewerCamera.pixelRect.height * .75f;

			foreach (Vector3 vec in corners)
			{
				Vector3 screenPoint = VRView.viewerCamera.WorldToScreenPoint(vec);
				if (screenPoint.x > minX && screenPoint.x < maxX && screenPoint.y > minY && screenPoint.y < maxY)
					return true;
			}
			return false;
		}
	}
	
	private RectTransform m_CaptureWindowRect;

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(0.6f, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		preventFrontBackResize = true;
		preventLeftRightResize = true;
		dynamicFaceAdjustment = false;

		m_ProfilerWindow = instantiateUI(m_ProfilerWindowPrefab).transform;
		m_ProfilerWindow.SetParent(m_WorkspaceUI.topFaceContainer,false);
		m_ProfilerWindow.localPosition = new Vector3(0f, -0.007f, -0.5f);
		m_ProfilerWindow.localRotation = Quaternion.Euler(90f, 0f, 0f);
		m_ProfilerWindow.localScale = new Vector3(1f, 1f, 1f);

		var bounds = contentBounds;
		var size = bounds.size;
		size.z = 0.1f;
		bounds.size = size;
		contentBounds = bounds;

		UnityEditorInternal.ProfilerDriver.profileEditor = false;

		m_CaptureWindowRect = GetComponentInChildren<EditorWindowCapture>().GetComponent<RectTransform>();
	}

	void Update()
	{
		UnityEditorInternal.ProfilerDriver.profileEditor = inView;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		UnityEditorInternal.ProfilerDriver.profileEditor = false;
	}
#endif
}