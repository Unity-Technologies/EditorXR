using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class HierarchyWorkspace : Workspace, IFilterUI, IConnectInterfaces
{
	const float kYBounds = 0.2f;
	const float kScrollMargin = 0.03f;

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_FilterPrefab;

	HierarchyUI m_HierarchyUI;
	FilterUI m_FilterUI;

	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;
	HierarchyData m_SelectedRow;

	bool m_HierarchyPanelDragging;
	Transform m_HierarchyPanelHighlightContainer;

	public ConnectInterfacesDelegate connectInterfaces { get; set; }

	public HierarchyData[] hierarchyData
	{
		private get { return m_HierarchyUI.hierarchyListView.data; }
		set
		{
			var oldData = m_HierarchyUI.hierarchyListView.data;
			if (oldData.Length > 0)
				CopyExpandStates(oldData[0], value[0]);

			m_HierarchyUI.hierarchyListView.data = value;
		}
	}

	public List<string> filterList { set { m_FilterUI.filterList = value; } }

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		topPanelDividerOffset = -0.2875f; // enable & position the top-divider(mask) slightly to the left of workspace center

		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_HierarchyUI = contentPrefab.GetComponent<HierarchyUI>();

		m_FilterUI = U.Object.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();

		var hierarchyListView = m_HierarchyUI.hierarchyListView;
		hierarchyListView.selectRow = SelectRow;
		hierarchyListView.data = new HierarchyData[0];

		var handle = m_HierarchyUI.scrollHandle;
		// Scroll Handle shouldn't move on bounds change
		handle.transform.parent = m_WorkspaceUI.sceneContainer;

		handle.dragStarted += OnScrollDragStarted;
		handle.dragging += OnScrollDragging;
		handle.dragEnded += OnScrollDragEnded;

		// Hookup highlighting calls
		//handle.dragStarted += OnFolderPanelDragHighlightBegin;
		//handle.dragEnded += OnFolderPanelDragHighlightEnd;
		//handle.hoverStarted += OnFolderPanelHoverHighlightBegin;
		//handle.hoverEnded += OnFolderPanelHoverHighlightEnd;

		// Assign highlight references
		m_HierarchyPanelHighlightContainer = m_HierarchyUI.highlight.transform.parent.transform;

		// Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		const float kSideScrollBoundsShrinkAmount = 0.03f;
		const float depthCompensation = 0.1375f;

		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.y = kYBounds;
		size.z = size.z - depthCompensation;
		bounds.size = size;
		bounds.center = Vector3.zero;

		var halfScrollMargin = kScrollMargin * 0.5f;
		var doubleScrollMargin = kScrollMargin * 2;
		var folderScrollHandleXPositionOffset = 0.025f;
		var folderScrollHandleXScaleOffset = 0.015f;

		var scrollHandleTransform = m_HierarchyUI.scrollHandle.transform;
		scrollHandleTransform.localPosition = new Vector3(-halfScrollMargin + folderScrollHandleXPositionOffset, -scrollHandleTransform.localScale.y * 0.5f, 0);
		scrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin + folderScrollHandleXScaleOffset, scrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var folderListView = m_HierarchyUI.hierarchyListView;
		size.x -= kSideScrollBoundsShrinkAmount; // set narrow x bounds for scrolling region on left side of folder list view
		bounds.size = size;
		folderListView.bounds = bounds;
		folderListView.PreCompute(); // Compute item size
		const float kFolderListShrinkAmount = kSideScrollBoundsShrinkAmount / 2.2f; // Empirically determined value to allow for scroll borders
		folderListView.transform.localPosition = new Vector3(kFolderListShrinkAmount, folderListView.itemSize.y * 0.5f, 0); // Center in Y

		m_HierarchyPanelHighlightContainer.localScale = new Vector3(size.x + kSideScrollBoundsShrinkAmount, 1f, size.z);

		size = contentBounds.size;
		size.z = size.z - depthCompensation;
		bounds.size = size;
	}

	void SelectRow(HierarchyData data)
	{
		if (data == m_SelectedRow)
			return;

		m_SelectedRow = data;
		m_HierarchyUI.hierarchyListView.ClearSelected();
		data.selected = true;

		// TODO: Set selection
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		m_ScrollOffsetStart = m_HierarchyUI.hierarchyListView.scrollOffset;
		m_HierarchyUI.hierarchyListView.OnBeginScrolling();
	}

	void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
		m_ScrollOffsetStart = m_HierarchyUI.hierarchyListView.scrollOffset;
		m_HierarchyUI.hierarchyListView.OnScrollEnded();
	}

	void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		m_HierarchyUI.hierarchyListView.scrollOffset = scrollOffset;
	}

	//void OnFolderPanelDragHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	//{
	//	m_FolderPanelDragging = true;
	//	m_HierarchyUI.folderPanelHighlight.visible = true;
	//}

	//void OnFolderPanelDragHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	//{
	//	m_FolderPanelDragging = false;
	//	m_HierarchyUI.folderPanelHighlight.visible = false;
	//}

	//void OnFolderPanelHoverHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	//{
	//	m_HierarchyUI.folderPanelHighlight.visible = true;
	//}

	//void OnFolderPanelHoverHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	//{
	//	if (!m_FolderPanelDragging)
	//		m_HierarchyUI.folderPanelHighlight.visible = false;
	//}

	bool TestFilter(string type)
	{
		return FilterUI.TestFilter(m_FilterUI.searchQuery, type);
	}

	HierarchyData GetFolderDataByInstanceID(HierarchyData data, int instanceID)
	{
		if (data.instanceID == instanceID)
			return data;

		if (data.children != null)
		{
			foreach (var child in data.children)
			{
				var folder = GetFolderDataByInstanceID(child, instanceID);
				if (folder != null)
					return folder;
			}
		}
		return null;
	}

	// In case a folder was moved up the hierarchy, we must search the entire destination root for every source folder
	void CopyExpandStates(HierarchyData source, HierarchyData destinationRoot)
	{
		var match = GetFolderDataByInstanceID(destinationRoot, source.instanceID);
		if (match != null)
			match.expanded = source.expanded;

		if (source.children != null)
		{
			foreach (var child in source.children)
			{
				CopyExpandStates(child, destinationRoot);
			}
		}
	}
}