#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	[MainMenuItem("Project", "Workspaces", "Manage the assets that belong to your project")]
	sealed class ProjectWorkspace : Workspace, IUsesProjectFolderData, IFilterUI
	{
		const float k_LeftPaneRatio = 0.3333333f; // Size of left pane relative to workspace bounds
		const float k_YBounds = 0.2f;

		const float k_MinScale = 0.04f;
		const float k_MaxScale = 0.09f;

		bool m_AssetGridDragging;
		bool m_FolderPanelDragging;

		[SerializeField]
		GameObject m_ContentPrefab;

		[SerializeField]
		GameObject m_SliderPrefab;

		[SerializeField]
		GameObject m_FilterPrefab;

		ProjectUI m_ProjectUI;
		FilterUI m_FilterUI;

		List<FolderData> m_FolderData;
		List<string> m_FilterList;

		public List<FolderData> folderData
		{
			set
			{
				m_FolderData = value;

				if (m_ProjectUI)
					m_ProjectUI.folderListView.data = value;
			}
		}

		public List<string> filterList
		{
			set
			{
				m_FilterList = value;
				m_FilterList.Sort();

				if (m_FilterUI)
					m_FilterUI.filterList = value;
			}
		}

		public string searchQuery { get { return m_FilterUI.searchQuery; } }

		public override void Setup()
		{
			// Initial bounds must be set before the base.Setup() is called
			minBounds = new Vector3(MinBounds.x, MinBounds.y, 0.5f);
			m_CustomStartingBounds = minBounds;

			base.Setup();

			topPanelDividerOffset = -0.2875f; // enable & position the top-divider(mask) slightly to the left of workspace center

			var contentPrefab = ObjectUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
			m_ProjectUI = contentPrefab.GetComponent<ProjectUI>();

			var assetGridView = m_ProjectUI.assetGridView;
			this.ConnectInterfaces(assetGridView);
			assetGridView.matchesFilter = this.MatchesFilter;
			assetGridView.data = new List<AssetData>();

			var folderListView = m_ProjectUI.folderListView;
			this.ConnectInterfaces(folderListView);
			folderListView.selectFolder = SelectFolder;
			folderData = m_FolderData;

			m_FilterUI = ObjectUtils.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
			foreach (var mb in m_FilterUI.GetComponentsInChildren<MonoBehaviour>())
			{
				this.ConnectInterfaces(mb);
			}
			filterList = m_FilterList;

			var sliderObject = ObjectUtils.Instantiate(m_SliderPrefab, m_WorkspaceUI.frontPanel, false);
			var zoomSlider = sliderObject.GetComponent<ZoomSliderUI>();
			zoomSlider.zoomSlider.minValue = Mathf.Log10(k_MinScale);
			zoomSlider.zoomSlider.maxValue = Mathf.Log10(k_MaxScale);
			zoomSlider.zoomSlider.value = Mathf.Log10(m_ProjectUI.assetGridView.scaleFactor);
			zoomSlider.sliding += Scale;
			foreach (var mb in zoomSlider.GetComponentsInChildren<MonoBehaviour>())
			{
				this.ConnectInterfaces(mb);
			}

			var zoomTooltip = sliderObject.GetComponentInChildren<Tooltip>();
			if (zoomTooltip)
				zoomTooltip.tooltipText = "Drag the Handle to Zoom the Asset Grid";

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

			// Propagate initial bounds
			OnBoundsChanged();
		}

		protected override void OnBoundsChanged()
		{
			const float kScrollHandleHeight = 0.001f;
			const float kScrollHandleYPosition = -0.002f;

			var bounds = contentBounds;
			var size = bounds.size;
			bounds.center = Vector3.zero;

			var sizeX = size.x * k_LeftPaneRatio + HighlightMargin;
			var sizeZ = size.z - FaceMargin + HighlightMargin;
			bounds.size = new Vector3(sizeX - FaceMargin, k_YBounds, sizeZ - FaceMargin);

			var xOffset = (contentBounds.size.x - sizeX - FaceMargin) * -0.5f - HighlightMargin * 0.5f;

			var folderScrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
			folderScrollHandleTransform.localPosition = new Vector3(xOffset, kScrollHandleYPosition, 0);
			folderScrollHandleTransform.localScale = new Vector3(sizeX, kScrollHandleHeight, sizeZ);

			var folderListView = m_ProjectUI.folderListView;
			folderListView.bounds = bounds;
			folderListView.transform.localPosition = new Vector3(xOffset, folderListView.itemSize.y * 0.5f, 0); // Center in Y

			sizeX = contentBounds.size.x * (1 - k_LeftPaneRatio) - FaceMargin;
			xOffset = (contentBounds.size.x - sizeX - FaceMargin) * 0.5f;
			sizeX += HighlightMargin;
			bounds.size = new Vector3(sizeX - FaceMargin, k_YBounds, sizeZ - FaceMargin);

			var assetScrollHandleTransform = m_ProjectUI.assetScrollHandle.transform;
			assetScrollHandleTransform.localPosition = new Vector3(xOffset, kScrollHandleYPosition, 0);
			assetScrollHandleTransform.localScale = new Vector3(sizeX, kScrollHandleHeight, sizeZ);

			var assetListView = m_ProjectUI.assetGridView;
			assetListView.bounds = bounds;
			assetListView.transform.localPosition = Vector3.right * xOffset;
		}

		void SelectFolder(FolderData data)
		{
			m_ProjectUI.assetGridView.data = data.assets;
			m_ProjectUI.assetGridView.scrollOffset = 0;
		}

		void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData)
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.OnBeginScrolling();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.OnBeginScrolling();
		}

		void OnScrollDragging(BaseHandle handle, HandleEventData eventData)
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
		}

		void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData)
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.OnScrollEnded();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.OnScrollEnded();
		}

		void OnAssetGridDragHighlightBegin(BaseHandle handle, HandleEventData eventData)
		{
			m_AssetGridDragging = true;
			m_ProjectUI.assetGridHighlight.visible = true;
		}

		void OnAssetGridDragHighlightEnd(BaseHandle handle, HandleEventData eventData)
		{
			m_AssetGridDragging = false;
			m_ProjectUI.assetGridHighlight.visible = false;
		}

		void OnAssetGridHoverHighlightBegin(BaseHandle handle, HandleEventData eventData)
		{
			m_ProjectUI.assetGridHighlight.visible = true;
		}

		void OnAssetGridHoverHighlightEnd(BaseHandle handle, HandleEventData eventData)
		{
			if (!m_AssetGridDragging)
				m_ProjectUI.assetGridHighlight.visible = false;
		}

		void OnFolderPanelDragHighlightBegin(BaseHandle handle, HandleEventData eventData)
		{
			m_FolderPanelDragging = true;
			m_ProjectUI.folderPanelHighlight.visible = true;
		}

		void OnFolderPanelDragHighlightEnd(BaseHandle handle, HandleEventData eventData)
		{
			m_FolderPanelDragging = false;
			m_ProjectUI.folderPanelHighlight.visible = false;
		}

		void OnFolderPanelHoverHighlightBegin(BaseHandle handle, HandleEventData eventData)
		{
			m_ProjectUI.folderPanelHighlight.visible = true;
		}

		void OnFolderPanelHoverHighlightEnd(BaseHandle handle, HandleEventData eventData)
		{
			if (!m_FolderPanelDragging)
				m_ProjectUI.folderPanelHighlight.visible = false;
		}

		void Scale(float value)
		{
			m_ProjectUI.assetGridView.scaleFactor = Mathf.Pow(10, value);
		}
	}
}
#endif
