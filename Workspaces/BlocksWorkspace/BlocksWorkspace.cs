#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

namespace BlocksImporter
{
    [MainMenuItem("Blocks", "Workspaces", "Import models from Google Blocks")]
    sealed class BlocksWorkspace : Workspace, ISerializeWorkspace
    {
        const float k_YBounds = 0.2f;

        const float k_MinScale = 0.05f;
        const float k_MaxScale = 0.2f;

        [Serializable]
        class Preferences
        {
            [SerializeField]
            float m_ScaleFactor;

            public float scaleFactor
            {
                get { return m_ScaleFactor; }
                set { m_ScaleFactor = value; }
            }
        }

        bool m_Scrolling;

        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_SliderPrefab;

        BlocksUI m_BlocksUI;
        ZoomSliderUI m_ZoomSliderUI;

        public List<BlocksAsset> assetData
        {
            set
            {
                if (m_BlocksUI)
                    m_BlocksUI.gridView.data = value;
            }
        }

        public override void Setup()
        {
            // Initial bounds must be set before the base.Setup() is called
            minBounds = new Vector3(MinBounds.x, k_YBounds, 0.5f);

            base.Setup();

            var contentPrefab = ObjectUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
            m_BlocksUI = contentPrefab.GetComponent<BlocksUI>();

            var gridView = m_BlocksUI.gridView;
            this.ConnectInterfaces(gridView);
            gridView.matchesFilter = s => true;
            assetData = new List<BlocksAsset>();

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

            var scrollHandle = m_BlocksUI.scrollHandle;
            var scrollHandleTransform = scrollHandle.transform;
            scrollHandleTransform.SetParent(m_WorkspaceUI.topFaceContainer);
            scrollHandleTransform.localScale = new Vector3(1.03f, 0.02f, 1.02f); // Extra space for scrolling
            scrollHandleTransform.localPosition = new Vector3(0f, -0.015f, 0f); // Offset from content for collision purposes

            scrollHandle.dragStarted += OnScrollDragStarted;
            scrollHandle.dragging += OnScrollDragging;
            scrollHandle.dragEnded += OnScrollDragEnded;
            m_BlocksUI.scrollHandle.hoverStarted += OnScrollHoverStarted;
            m_BlocksUI.scrollHandle.hoverEnded += OnScrollHoverEnded;

            // Propagate initial bounds
            OnBoundsChanged();
        }

        public object OnSerializeWorkspace()
        {
            var preferences = new Preferences();
            preferences.scaleFactor = m_BlocksUI.gridView.scaleFactor;
            return preferences;
        }

        public void OnDeserializeWorkspace(object obj)
        {
            var preferences = (Preferences)obj;
            m_BlocksUI.gridView.scaleFactor = preferences.scaleFactor;
            UpdateZoomSliderValue();
        }

        protected override void OnBoundsChanged()
        {
            var size = contentBounds.size;
            var gridView = m_BlocksUI.gridView;
            size.x -= FaceMargin * 2; // Shrink the content width, so that there is space allowed to grab and scroll
            size.z -= FaceMargin * 2; // Reduce the height of the inspector contents as to fit within the bounds of the workspace
            gridView.size = size;
        }

        void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = true;

            m_WorkspaceUI.topHighlight.visible = true;
            m_WorkspaceUI.amplifyTopHighlight = false;

            m_BlocksUI.gridView.OnBeginScrolling();
        }

        void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_BlocksUI.gridView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
        }

        void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = false;

            m_WorkspaceUI.topHighlight.visible = false;

            m_BlocksUI.gridView.OnScrollEnded();
        }

        void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            if (!m_Scrolling)
            {
                m_WorkspaceUI.topHighlight.visible = true;
                m_WorkspaceUI.amplifyTopHighlight = true;
            }
        }

        void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            if (!m_Scrolling && m_WorkspaceUI.gameObject.activeInHierarchy) // Check active to prevent errors in OnDestroy
            {
                m_WorkspaceUI.topHighlight.visible = false;
                m_WorkspaceUI.amplifyTopHighlight = false;
            }
        }

        void Scale(float value)
        {
            m_BlocksUI.gridView.scaleFactor = Mathf.Pow(10, value);
        }

        void UpdateZoomSliderValue()
        {
            m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(m_BlocksUI.gridView.scaleFactor);
        }
    }
}
#endif
