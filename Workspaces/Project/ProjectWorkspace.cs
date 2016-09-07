using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class ProjectWorkspace : Workspace
{
	private const float kLeftPaneRatio = 0.3333333f; //Size of left pane relative to workspace bounds
	private const float kPaneMargin = 0.01f;
	private const float kPanelMargin = 0.01f;
	private const float kScrollMargin = 0.03f;
	private const float kYBounds = 0.2f;

	private const float kMinScale = 0.03f;
	private const float kMaxScale = 0.2f;

	[SerializeField]
	private GameObject m_ContentPrefab;

	[SerializeField]
	private GameObject m_SliderPrefab;

	[SerializeField]
	private GameObject m_FilterPrefab;

	private ProjectUI m_ProjectUI;
	private FilterUI m_FilterUI;

	private Vector3 m_ScrollStart;
	private float m_ScrollOffsetStart;

	public override void Setup()
	{
		base.Setup();
		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();

		var filterPrefab = U.Object.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false);
		m_FilterUI = filterPrefab.GetComponent<FilterUI>();

		var sliderPrefab = U.Object.Instantiate(m_SliderPrefab, m_WorkspaceUI.frontPanel, false);
		var zoomSlider = sliderPrefab.GetComponent<ZoomSliderUI>();
		zoomSlider.zoomSlider.minValue = kMinScale;
		zoomSlider.zoomSlider.maxValue = kMaxScale;
		zoomSlider.zoomSlider.value = m_ProjectUI.assetListView.scaleFactor;
		zoomSlider.sliding += Scale;

		m_ProjectUI.assetListView.testFilter = TestFilter;

#if UNITY_EDITOR
		EditorApplication.projectWindowChanged += SetupFolderList;
		SetupFolderList();
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
			// Scroll Handle shouldn't move on bounds change
			handle.transform.parent = m_WorkspaceUI.sceneContainer;

			handle.handleDragging += OnScrollBeginDrag;
			handle.handleDrag += OnScrollDrag;
			handle.handleDragged += OnScrollEndDrag;
			handle.hovering += OnScrollHoverEnter;
			handle.hovered += OnScrollHoverExit;
		}

		m_WorkspaceUI.showBounds = false;

		// Propagate initial bounds
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

		var halfScrollMargin = kScrollMargin * 0.5f;
		var doubleScrollMargin = kScrollMargin * 2;

		var xOffset = (contentBounds.size.x - size.x + kPaneMargin) * -0.5f;

		var folderScrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
		folderScrollHandleTransform.localPosition = new Vector3(xOffset - halfScrollMargin, -folderScrollHandleTransform.localScale.y * 0.5f, 0);
		folderScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, folderScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var folderListView = m_ProjectUI.folderListView;
		folderListView.bounds = bounds;
		folderListView.PreCompute(); // Compute item size
		folderListView.transform.localPosition = new Vector3(xOffset, folderListView.itemSize.y * 0.5f, 0);
		folderListView.selectFolder = SelectFolder;

		var folderPanel = m_ProjectUI.folderPanel;
		folderPanel.transform.localPosition = xOffset * Vector3.right;
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);

		size = contentBounds.size;
		size.x -= kPaneMargin * 2;
		size.x *= 1 - kLeftPaneRatio;
		bounds.size = size;

		xOffset = (contentBounds.size.x - size.x + kPaneMargin) * 0.5f;
			
		var assetScrollHandleTransform = m_ProjectUI.assetScrollHandle.transform;
		assetScrollHandleTransform.localPosition = new Vector3(xOffset + halfScrollMargin, -assetScrollHandleTransform.localScale.y * 0.5f);
		assetScrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin, assetScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

		var assetListView = m_ProjectUI.assetListView;
		assetListView.bounds = bounds;
		assetListView.PreCompute(); // Compute item size
		assetListView.transform.localPosition = Vector3.right * xOffset;

		var assetPanel = m_ProjectUI.assetPanel;
		assetPanel.transform.localPosition = xOffset * Vector3.right;
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + kPanelMargin);
		assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + kPanelMargin);
	}

	private void SelectFolder(FolderData data)
	{
		foreach (var folderData in m_ProjectUI.folderListView.listData)
			folderData.ClearSelected();
		data.selected = true;
		m_ProjectUI.assetListView.listData = data.assets;
	}

	private void OnScrollBeginDrag(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		if (handle == m_ProjectUI.folderScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnBeginScrolling();
		}
		else if (handle == m_ProjectUI.assetScrollHandle)
		{
			m_ScrollOffsetStart = m_ProjectUI.assetListView.scrollOffset;
			m_ProjectUI.assetListView.OnBeginScrolling();
		}
	}

	private void OnScrollDrag(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	private void OnScrollEndDrag(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle,eventData);
		if (handle == m_ProjectUI.folderScrollHandle) {
			m_ScrollOffsetStart = m_ProjectUI.folderListView.scrollOffset;
			m_ProjectUI.folderListView.OnEndScrolling();
		} else if (handle == m_ProjectUI.assetScrollHandle) {
			m_ScrollOffsetStart = m_ProjectUI.assetListView.scrollOffset;
			m_ProjectUI.assetListView.OnEndScrolling();
		}
	}

	private void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		if (handle == m_ProjectUI.folderScrollHandle)
			m_ProjectUI.folderListView.scrollOffset = scrollOffset;
		else if(handle == m_ProjectUI.assetScrollHandle)
			m_ProjectUI.assetListView.scrollOffset = scrollOffset;
	}

	private void OnScrollHoverEnter(BaseHandle handle, HandleEventData eventData = default(HandleEventData)) {
		setHighlight(handle.gameObject, true);
	}

	private void OnScrollHoverExit(BaseHandle handle, HandleEventData eventData = default(HandleEventData)) {
		setHighlight(handle.gameObject, false);
	}

	private void Scale(float value)
	{
		m_ProjectUI.assetListView.scaleFactor = value;
	}

	private bool TestFilter(string type)
	{
		return FilterUI.TestFilter(m_FilterUI.searchQuery, type);
	}

#if UNITY_EDITOR
	private void SetupFolderList()
	{
		var assetTypes = new HashSet<string>();
		var folderData = new[] { new FolderData(assetTypes) { expanded = true } };
		m_ProjectUI.folderListView.listData = folderData;

		if (folderData.Length > 0)
			SelectFolder(folderData[0]);

		m_FilterUI.filterTypes = assetTypes.ToList();
	}

	private void OnDestroy()
	{
		EditorApplication.projectWindowChanged -= SetupFolderList;
	}
#endif
}