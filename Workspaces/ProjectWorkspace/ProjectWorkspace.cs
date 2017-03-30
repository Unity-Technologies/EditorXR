#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	[MainMenuItem("Project", "Workspaces", "Manage the assets that belong to your project")]
	sealed class ProjectWorkspace : Workspace, IUsesProjectFolderData, IFilterUI, ISerializeWorkspace
	{
		const float k_LeftPaneRatio = 0.3333333f; // Size of left pane relative to workspace bounds
		const float k_PaneMargin = 0.01f;
		const float k_PanelMargin = 0.01f;
		const float k_ScrollMargin = 0.03f;
		const float k_YBounds = 0.2f;

		const float k_MinScale = 0.04f;
		const float k_MaxScale = 0.09f;

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
		ZoomSliderUI m_ZoomSliderUI;

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
				m_FilterList.Sort();

				if (m_FilterUI)
					m_FilterUI.filterList = value;
			}
		}

		List<string> m_FilterList;

		public string searchQuery { get { return m_FilterUI.searchQuery; } }

		[Serializable]
		class Preferences
		{
			public float scaleFactor;
		}

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
			m_ZoomSliderUI = sliderObject.GetComponent<ZoomSliderUI>();
			m_ZoomSliderUI.zoomSlider.minValue = Mathf.Log10(k_MinScale);
			m_ZoomSliderUI.zoomSlider.maxValue = Mathf.Log10(k_MaxScale);
			m_ZoomSliderUI.sliding += Scale;
			UpdateZoomSliderValue();
			foreach (var mb in m_ZoomSliderUI.GetComponentsInChildren<MonoBehaviour>())
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

			// Assign highlight references
			m_FolderPanelHighlightContainer = m_ProjectUI.folderPanelHighlight.transform.parent.transform;
			m_AssetGridHighlightContainer = m_ProjectUI.assetGridHighlight.transform.parent.transform;

			// Propagate initial bounds
			OnBoundsChanged();
		}

		public object OnSerializeWorkspace()
		{
			var preferences = new Preferences();
			preferences.scaleFactor = m_ProjectUI.assetGridView.scaleFactor;
			return preferences;
		}

		public void OnDeserializeWorkspace(object obj)
		{
			var preferences = (Preferences)obj;
			m_ProjectUI.assetGridView.scaleFactor = preferences.scaleFactor;
			UpdateZoomSliderValue();
		}

		protected override void OnBoundsChanged()
		{
			const float kSideScrollBoundsShrinkAmount = 0.03f;
			const float depthCompensation = 0.1375f;

			var bounds = contentBounds;
			var size = bounds.size;
			size.x -= k_PaneMargin * 2;
			size.x *= k_LeftPaneRatio;
			size.y = k_YBounds;
			size.z = size.z - depthCompensation;
			bounds.size = size;
			bounds.center = Vector3.zero;

			var halfScrollMargin = k_ScrollMargin * 0.5f;
			var doubleScrollMargin = k_ScrollMargin * 2;
			var xOffset = (contentBounds.size.x - size.x + k_PaneMargin) * -0.5f;
			var folderScrollHandleXPositionOffset = 0.025f;
			var folderScrollHandleXScaleOffset = 0.015f;

			var folderScrollHandleTransform = m_ProjectUI.folderScrollHandle.transform;
			folderScrollHandleTransform.localPosition = new Vector3(xOffset - halfScrollMargin + folderScrollHandleXPositionOffset, -folderScrollHandleTransform.localScale.y * 0.5f, 0);
			folderScrollHandleTransform.localScale = new Vector3(size.x + k_ScrollMargin + folderScrollHandleXScaleOffset, folderScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

			var folderListView = m_ProjectUI.folderListView;
			size.x -= kSideScrollBoundsShrinkAmount; // set narrow x bounds for scrolling region on left side of folder list view
			bounds.size = size;
			folderListView.bounds = bounds;
			const float kFolderListShrinkAmount = kSideScrollBoundsShrinkAmount / 2.2f; // Empirically determined value to allow for scroll borders
			folderListView.transform.localPosition = new Vector3(xOffset + kFolderListShrinkAmount, folderListView.itemSize.y * 0.5f, 0); // Center in Y

			var folderPanel = m_ProjectUI.folderPanel;
			folderPanel.transform.localPosition = xOffset * Vector3.right;
			folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + k_PanelMargin);
			folderPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + k_PanelMargin);

			m_FolderPanelHighlightContainer.localScale = new Vector3(size.x + kSideScrollBoundsShrinkAmount, 1f, size.z);

			m_FolderPanelHighlightContainer.localScale = new Vector3(size.x + kSideScrollBoundsShrinkAmount, 1f, size.z);

			size = contentBounds.size;
			size.x -= k_PaneMargin * 2; // Reserve space for scroll on both sides
			size.x *= 1 - k_LeftPaneRatio;
			size.z = size.z - depthCompensation;
			bounds.size = size;

			xOffset = (contentBounds.size.x - size.x + k_PaneMargin) * 0.5f;

			var assetScrollHandleTransform = m_ProjectUI.assetScrollHandle.transform;
			assetScrollHandleTransform.localPosition = new Vector3(xOffset + halfScrollMargin, -assetScrollHandleTransform.localScale.y * 0.5f);
			assetScrollHandleTransform.localScale = new Vector3(size.x + k_ScrollMargin, assetScrollHandleTransform.localScale.y, size.z + doubleScrollMargin);

			var assetListView = m_ProjectUI.assetGridView;
			assetListView.bounds = bounds;
			assetListView.transform.localPosition = Vector3.right * xOffset;


			var assetPanel = m_ProjectUI.assetPanel;
			assetPanel.transform.localPosition = xOffset * Vector3.right;
			assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + k_PanelMargin);
			assetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z + k_PanelMargin);

			m_AssetGridHighlightContainer.localScale = new Vector3(size.x, 1f, size.z);
		}


		void SelectFolder(FolderData data)
		{
			m_ProjectUI.assetGridView.data = data.assets;
			m_ProjectUI.assetGridView.scrollOffset = 0;
		}

		void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.OnBeginScrolling();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.OnBeginScrolling();
		}

		void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
		}

		void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
		{
			if (handle == m_ProjectUI.folderScrollHandle)
				m_ProjectUI.folderListView.OnScrollEnded();
			else if (handle == m_ProjectUI.assetScrollHandle)
				m_ProjectUI.assetGridView.OnScrollEnded();
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
			m_ProjectUI.assetGridView.scaleFactor = Mathf.Pow(10, value);
		}

		void UpdateZoomSliderValue()
		{
			m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(m_ProjectUI.assetGridView.scaleFactor);
		}
	}
}
#endif
