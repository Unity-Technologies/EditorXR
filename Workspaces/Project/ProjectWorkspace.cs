using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Menus;
using UnityEngine.Experimental.EditorVR.Utilities;
using UnityEngine.Experimental.EditorVR.Workspaces;

[MainMenuItem("Project", "Workspaces", "Manage the assets that belong to your project")]
public class ProjectWorkspace : Workspace, IUsesProjectFolderData, IFilterUI
{
	const float kLeftPaneRatio = 0.3333333f; // Size of left pane relative to workspace bounds
	const float kPaneMargin = 0.01f;
	const float kPanelMargin = 0.01f;
	const float kScrollMargin = 0.03f;
	const float kYBounds = 0.2f;

	const float kMinScale = 0.03f;
	const float kMaxScale = 0.2f;

	bool m_AssetGridDragging;
	bool m_FolderPanelDragging;
	Transform m_AssetGridHighlightContainer;
	Transform m_FolderPanelHighlightContainer;

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_SliderPrefab;

	[SerializeField]
	GameObject m_FilterPrefab;

	ProjectUI m_ProjectUI;
	FilterUI m_FilterUI;

	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;

	public List<FolderData> folderData
	{
		set
		{
			m_FolderData = value;

			if (m_ProjectUI)
				m_ProjectUI.folderListView.data = value;
		}
	}
	List<FolderData> m_FolderData;

	public List<string> filterList
	{
		set
		{
			m_FilterList = value;
			if (m_FilterUI)
				m_FilterUI.filterList = value;
		}
	}
	List<string> m_FilterList;

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		topPanelDividerOffset = -0.2875f; // enable & position the top-divider(mask) slightly to the left of workspace center

		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();

		var assetGridView = m_ProjectUI.assetGridView;
		assetGridView.testFilter = TestFilter;
		assetGridView.data = new List<AssetData>();
		connectInterfaces(assetGridView);

		var folderListView = m_ProjectUI.folderListView;
		folderListView.selectFolder = SelectFolder;
		folderData = m_FolderData;

		m_FilterUI = U.Object.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
		foreach (var mb in m_FilterUI.GetComponentsInChildren<MonoBehaviour>())
		{
			connectInterfaces(mb);
		}
		filterList = m_FilterList;

		var sliderPrefab = U.Object.Instantiate(m_SliderPrefab, m_WorkspaceUI.frontPanel, false);
		var zoomSlider = sliderPrefab.GetComponent<ZoomSliderUI>();
		zoomSlider.zoomSlider.minValue = kMinScale;
		zoomSlider.zoomSlider.maxValue = kMaxScale;
		zoomSlider.zoomSlider.value = m_ProjectUI.assetGridView.scaleFactor;
		zoomSlider.sliding += Scale;
		foreach (var mb in zoomSlider.GetComponentsInChildren<MonoBehaviour>())
		{
			connectInterfaces(mb);
		}

		var scrollHandles = new[]
		{
			m_ProjectUI.folderScrollHandle,
			m_ProjectUI.assetScrollHandle
		};
		foreach (var handle in scrollHandles)
		{
			// Scroll Handle shouldn't move on bounds change
			handle.transform.parent = m_WorkspaceUI.sceneContainer;

			handle.dragStarted += OnScrollDragStarted;
			handle.dragging += OnScrollDragging;
			handle.dragEnded += OnScrollDragEnded;
		}

		// Hookup highlighting calls
		m_ProjectUI.assetScrollHandle.dragStarted += OnAssetGridDragHighlightBegin;
		m_ProjectUI.assetScrollHandle.dragEnded += OnAssetGridDragHighlightEnd;
		m_ProjectUI.assetScrollHandle.hoverStarted += OnAssetGridHoverHighlightBegin;
		m_ProjectUI.assetScrollHandle.hoverEnded += OnAssetGridHoverHighlightEnd;
		m_ProjectUI.folderScrollHandle.dragStarted += OnFolderPanelDragHighlightBegin;
		m_ProjectUI.folderScrollHandle.dragEnded += OnFolderPanelDragHighlightEnd;
		m_ProjectUI.folderScrollHandle.hoverStarted += OnFolderPanelHoverHighlightBegin;
		m_ProjectUI.folderScrollHandle.hoverEnded += OnFolderPanelHoverHighlightEnd;

		// Assign highlight references
		m_FolderPanelHighlightContainer = m_ProjectUI.folderPanelHighlight.transform.parent.transform;
		m_AssetGridHighlightContainer = m_ProjectUI.assetGridHighlight.transform.parent.transform;

		// Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		const float kSideScrollBoundsShrinkAmount = 0.03f;
		const float depthCompensation = 0.1375f;

		Bounds bounds = contentBounds;
		Vector3 size = bounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= kLeftPaneRatio;
		size.y = kYBounds;
		size.z = size.z - depthCompensation;
		bounds.size = size;
		bounds.center = Vector3.zero;

		var halfScrollMargin = kScrollMargin * 0.5f;
		var doubleScrollMargin = kScrollMargin * 2;
		var xOffset = (contentBounds.size.x - size.x + kPaneMargin) * -0.5f;
		var folderScrollHandleXPositionOffset = 0.025f;
		var folderScrollHandleXScaleOffset = 0.015f;

		var folderScrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
		folderScrollHandleTransform.localPosition = new Vector3(xOffset - halfScrollMargin + folderScrollHandleXPositionOffset, -folderScrollHandleTransform.localScale.y * 0.5f, 0);
		folderScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin + folderScrollHandleXScaleOffset, folderScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var folderListView = m_ProjectUI.folderListView;
		size.x -= kSideScrollBoundsShrinkAmount; // set narrow x bounds for scrolling region on left side of folder list view
		bounds.size = size;
		folderListView.bounds = bounds;
		const float kFolderListShrinkAmount = kSideScrollBoundsShrinkAmount / 2.2f; // Empirically determined value to allow for scroll borders
		folderListView.transform.localPosition = new Vector3(xOffset + kFolderListShrinkAmount, folderListView.itemSize.y * 0.5f, 0); // Center in Y

		var folderPanel = m_ProjectUI.folderPanel;
		folderPanel.transform.localPosition = xOffset * Vector3.right;
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);

		m_FolderPanelHighlightContainer.localScale = new Vector3(size.x + kSideScrollBoundsShrinkAmount, 1f, size.z);

		size = contentBounds.size;
		size.x -= kPaneMargin * 2; // Reserve space for scroll on both sides
		size.x *= 1 - kLeftPaneRatio;
		size.z = size.z - depthCompensation;
		bounds.size = size;

		xOffset = (contentBounds.size.x - size.x + kPaneMargin) * 0.5f;

		var assetScrollHandleTransform = m_ProjectUI.assetScrollHandle.transform;
		assetScrollHandleTransform.localPosition = new Vector3(xOffset + halfScrollMargin, -assetScrollHandleTransform.localScale.y * 0.5f);
		assetScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, assetScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var assetListView = m_ProjectUI.assetGridView;
		assetListView.bounds = bounds;
		assetListView.transform.localPosition = Vector3.right * xOffset;

		var assetPanel = m_ProjectUI.assetPanel;
		assetPanel.transform.localPosition = xOffset * Vector3.right;
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);

		m_AssetGridHighlightContainer.localScale = new Vector3(size.x, 1f, size.z);
	}

	void SelectFolder(FolderData data)
	{
		m_ProjectUI.assetGridView.data = data.assets;
		m_ProjectUI.assetGridView.scrollOffset = 0;
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		if (handle == m_ProjectUI.folderScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnBeginScrolling();
		}
		else if (handle == m_ProjectUI.assetScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.assetGridView.scrollOffset;
			m_ProjectUI.assetGridView.OnBeginScrolling();
		}
	}

	void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
		if (handle == m_ProjectUI.folderScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnScrollEnded();
		}
		else if (handle == m_ProjectUI.assetScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.assetGridView.scrollOffset;
			m_ProjectUI.assetGridView.OnScrollEnded();
		}
	}

	void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		if (handle == m_ProjectUI.folderScrollHandle)
			m_ProjectUI.folderListView.scrollOffset = scrollOffset;
		else if (handle == m_ProjectUI.assetScrollHandle)
			m_ProjectUI.assetGridView.scrollOffset = scrollOffset;
	}

	void OnAssetGridDragHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_AssetGridDragging = true;
		m_ProjectUI.assetGridHighlight.visible = true;
	}

	void OnAssetGridDragHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_AssetGridDragging = false;
		m_ProjectUI.assetGridHighlight.visible = false;
	}

	void OnAssetGridHoverHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ProjectUI.assetGridHighlight.visible = true;
	}

	void OnAssetGridHoverHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_AssetGridDragging)
			m_ProjectUI.assetGridHighlight.visible = false;
	}

	void OnFolderPanelDragHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_FolderPanelDragging = true;
		m_ProjectUI.folderPanelHighlight.visible = true;
	}

	void OnFolderPanelDragHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_FolderPanelDragging = false;
		m_ProjectUI.folderPanelHighlight.visible = false;
	}

	void OnFolderPanelHoverHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ProjectUI.folderPanelHighlight.visible = true;
	}

	void OnFolderPanelHoverHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_FolderPanelDragging)
			m_ProjectUI.folderPanelHighlight.visible = false;
	}

	void Scale(float value)
	{
		m_ProjectUI.assetGridView.scaleFactor = value;
	}

	bool TestFilter(string type)
	{
		return FilterUI.TestFilter(m_FilterUI.searchQuery, type);
	}
}