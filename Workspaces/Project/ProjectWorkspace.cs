using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;

public class ProjectWorkspace : Workspace
{
	private const float kLeftPaneRatio = 0.3333333f; //Size of left pane relative to workspace bounds
	private const float kPaneMargin = 0.01f;
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
		var folderData = new[]
		{
			new FolderData(Application.dataPath) {expanded = true},
		};
		m_ProjectUI.folderListView.listData = folderData;

		//TEMP: use select method when it exists
		if (folderData.Length > 0)
		{
			m_ProjectUI.assetListView.listData = folderData[0].assets;
		}
#else
		Debug.LogWarning("Project workspace does not work in builds");
		return;
#endif
		var scrollHandles = new []
		{
			m_ProjectUI.folderScrollHandle,
			m_ProjectUI.assetScrollHandle
		};
		foreach (var handle in scrollHandles)
		{
			//Scroll Handle shouldn't move on bounds change
			handle.transform.parent = m_WorkspaceUI.sceneContainer;

			handle.onHandleBeginDrag += OnScrollBeginDrag;
			handle.onHandleDrag += OnScrollDrag;
			handle.onHandleEndDrag += OnScrollEndDrag;
			handle.onHoverEnter += OnScrollHoverEnter;
			handle.onHoverExit += OnScrollHoverExit;
		}

		//Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= kLeftPaneRatio;
		size.y = kYBounds;
		bounds.size = size;
		bounds.center = Vector3.zero;
		
		var folderListView = m_ProjectUI.folderListView;
		folderListView.PreCompute(); //Compute item size
		folderListView.bounds = bounds;
		folderListView.transform.localPosition = (contentBounds.size.x - size.x + kPaneMargin) * 0.5f * Vector3.left + folderListView.itemSize.y * 0.5f * Vector3.up; ;
		folderListView.range = contentBounds.size.z;

		size = contentBounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= 1 - kLeftPaneRatio;
		bounds.size = size;

		var assetListView = m_ProjectUI.assetListView;
		assetListView.PreCompute(); //Compute item size
		assetListView.bounds = bounds;
		assetListView.transform.localPosition = (contentBounds.size.x - size.x + kPaneMargin) * 0.5f * Vector3.right + assetListView.itemSize.y * 0.5f * Vector3.up;
		assetListView.range = contentBounds.size.z;

		var scrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
		scrollHandleTransform.localScale = new Vector3(contentBounds.size.x, scrollHandleTransform.localScale.y, contentBounds.size.z);
	}

	private void OnScrollBeginDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		if (handle == m_ProjectUI.folderScrollHandle)
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
		else if (handle == m_ProjectUI.assetScrollHandle)
			m_ScrollOffsetStart = m_ProjectUI.assetListView.scrollOffset;
	}

	private void OnScrollDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		Scroll(handle, eventData);
	}

	private void OnScrollEndDrag(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData))
	{
		Scroll(handle,eventData);
	}

	private void Scroll(BaseHandle handle, HandleDragEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		if (handle == m_ProjectUI.folderScrollHandle)
			m_ProjectUI.folderListView.scrollOffset = scrollOffset;
		else if(handle == m_ProjectUI.assetScrollHandle)
			m_ProjectUI.assetListView.scrollOffset = scrollOffset;
	}

	private void OnScrollHoverEnter(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData)) {
		setHighlight(handle.gameObject, true);
	}

	private void OnScrollHoverExit(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData)) {
		setHighlight(handle.gameObject, false);
	}
}