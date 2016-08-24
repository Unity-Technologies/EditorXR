using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class ProjectWorkspace : Workspace
{
	private const float kLeftPaneRatio = 0.33333f; //Size of left pane relative to workspace bounds
	private const float kYBounds = 0.2f;

	[SerializeField]
	private GameObject m_ContentPrefab;

	private ProjectUI m_ProjectUI;

	private Vector3 m_ScrollStart;
	private float m_ScrollOffsetStart;

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.InstantiateAndSetActive(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();
#if UNITY_EDITOR
		m_ProjectUI.listView.listData = FolderData.GetAssetDataForPath(Application.dataPath);
#else
		Debug.LogWarning("Project workspace does not work in builds");
		return;
#endif
		//Set Scroll Handle
		var scrollHandle = m_ProjectUI.folderScrollHandle;
		//ControlBox shouldn't move with miniWorld
		scrollHandle.transform.parent = m_WorkspaceUI.sceneContainer;
		scrollHandle.transform.localPosition = Vector3.down * scrollHandle.transform.localScale.y;
		scrollHandle.onHandleBeginDrag += OnScrollBeginDrag;
		scrollHandle.onHandleDrag += OnScrollDrag;
		scrollHandle.onHandleEndDrag += OnScrollEndDrag;
		scrollHandle.onHoverEnter += OnScrollHoverEnter;
		scrollHandle.onHoverExit += OnScrollHoverExit;

		//Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.x *= kLeftPaneRatio;
		size.y = kYBounds;
		bounds.size = size;
		bounds.center = Vector3.zero;
		m_ProjectUI.listView.bounds = bounds;
		m_ProjectUI.listView.transform.localPosition = contentBounds.size.x * kLeftPaneRatio * Vector3.left;
		m_ProjectUI.listView.range = contentBounds.size.z;

		var scrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
		scrollHandleTransform.localScale = new Vector3(contentBounds.size.x, scrollHandleTransform.localScale.y, contentBounds.size.z);
	}

	private void OnScrollBeginDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		m_ScrollOffsetStart = m_ProjectUI.listView.scrollOffset;
	}

	private void OnScrollDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		Scroll(eventData);
	}

	private void OnScrollEndDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		Scroll(eventData);
	}

	private void Scroll(HandleDragEventData eventData)
	{
		m_ProjectUI.listView.scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
	}

	private void OnScrollHoverEnter(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData)) {
		setHighlight(handle.gameObject, true);
	}

	private void OnScrollHoverExit(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData)) {
		setHighlight(handle.gameObject, false);
	}
}