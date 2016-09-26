using UnityEngine;
using UnityEngine.VR.Workspaces;
using UnityEditor.VR;

public class ProfilerWorkspace : Workspace
{
	[SerializeField]
	private GameObject m_ProfilerWindowPrefab;

	private Transform m_ProfilerWindow;

	private bool m_InView;

	private const float kQuarter = 0.25f;
	private const float kThreeQuarters = 0.75f;

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
		m_InView = false;
    }

	void Update()
	{
		UpdateProfilerInView();
	}

	void UpdateProfilerInView()
	{
		RectTransform rect = GetComponentInChildren<EditorWindowCapture>().GetComponent<RectTransform>();
		Vector3[] corners = new Vector3[4];
		rect.GetWorldCorners(corners);

		float minX = VRView.viewerCamera.pixelRect.width * kQuarter;
		float minY = VRView.viewerCamera.pixelRect.height * kQuarter;
		float maxX = VRView.viewerCamera.pixelRect.width * kThreeQuarters;
		float maxY = VRView.viewerCamera.pixelRect.height * kThreeQuarters;

		m_InView = false;
		foreach(Vector3 vec in corners)
		{
			Vector3 screenPoint = VRView.viewerCamera.WorldToScreenPoint(vec);
			if(screenPoint.x > minX && screenPoint.x < maxX && screenPoint.y > minY && screenPoint.y < maxY)
			{
				m_InView = true;
				break;
			}
		}

		UnityEditorInternal.ProfilerDriver.profileEditor = m_InView;
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