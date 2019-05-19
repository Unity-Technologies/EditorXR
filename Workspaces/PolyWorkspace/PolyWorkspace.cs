using System;
using System.Collections;
using System.Threading;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEngine;

#if INCLUDE_POLY_TOOLKIT
using System.Collections.Generic;
using PolyToolkit;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
#else
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine.InputNew;
#endif

#if UNITY_EDITOR
using Unity.Labs.Utils;

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]
#endif

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
#if INCLUDE_POLY_TOOLKIT
    [MainMenuItem("Poly", "Workspaces", "Import models from Google Poly")]
    [SpatialMenuItem("Poly", "Workspaces", "Import models from Google Poly")]
    sealed class PolyWorkspace : Workspace, ISerializeWorkspace
    {
        const float k_MinScale = 0.05f;
        const float k_MaxScale = 0.2f;

        const float k_HighlightDelay = 0.05f;

        const float k_FilterUIWidth = 0.162f;

        const string k_Featured = "Featured";
        const string k_Newest = "Newest";

        const string k_Blocks = "Blocks";
        const string k_TiltBrush = "Tilt Brush";

        const string k_Medium = "Medium";
        const string k_Simple = "Simple";

        static readonly Vector3 k_MinBounds = new Vector3(1.094f, 0.2f, 0.5f);
        static readonly Vector3 k_ScrollHandleScale = new Vector3(1.03f, 0.02f, 1.02f); // Extra space for scrolling
        static readonly Vector3 k_ScrollHandlePosition = new Vector3(0f, -0.015f, 0f); // Offset from content for collision purposes

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

#pragma warning disable 649
        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_FilterUIPrefab;

        [SerializeField]
        GameObject m_SliderPrefab;
#pragma warning restore 649

        bool m_Scrolling;

        PolyUI m_PolyUI;
        FilterUI m_SortingUI;
        FilterUI m_FormatFilterUI;
        FilterUI m_ComplexityFilterUI;
        FilterUI m_CategoryFilterUI;
        ZoomSliderUI m_ZoomSliderUI;

        Coroutine m_HighlightDelayCoroutine;

        public List<PolyGridAsset> assetData
        {
            set
            {
                if (m_PolyUI)
                    m_PolyUI.gridView.data = value;
            }
        }

        public override void Setup()
        {
            // Initial bounds must be set before the base.Setup() is called
            minBounds = k_MinBounds;
            m_CustomStartingBounds = minBounds;

            base.Setup();

            var contentPrefab = EditorXRUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
            m_PolyUI = contentPrefab.GetComponent<PolyUI>();

            var gridView = m_PolyUI.gridView;
            this.ConnectInterfaces(gridView);
            assetData = new List<PolyGridAsset>();

            var sliderObject = EditorXRUtils.Instantiate(m_SliderPrefab, m_WorkspaceUI.frontPanel, false);
            m_ZoomSliderUI = sliderObject.GetComponent<ZoomSliderUI>();
            m_ZoomSliderUI.zoomSlider.minValue = Mathf.Log10(k_MinScale);
            m_ZoomSliderUI.zoomSlider.maxValue = Mathf.Log10(k_MaxScale);
            m_ZoomSliderUI.sliding += Scale;
            UpdateZoomSliderValue();
            foreach (var mb in m_ZoomSliderUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            SetupCategoryFilterUI();
            SetupComplextyFilterUI();
            SetupFormatFilterUI();
            SetupSortingUI();

            var zoomTooltip = sliderObject.GetComponentInChildren<Tooltip>();
            if (zoomTooltip)
                zoomTooltip.tooltipText = "Drag the Handle to Zoom the Asset Grid";

            var scrollHandle = m_PolyUI.scrollHandle;
            var scrollHandleTransform = scrollHandle.transform;
            scrollHandleTransform.SetParent(m_WorkspaceUI.topFaceContainer);
            scrollHandleTransform.localScale = k_ScrollHandleScale;
            scrollHandleTransform.localPosition = k_ScrollHandlePosition;

            scrollHandle.dragStarted += OnScrollDragStarted;
            scrollHandle.dragging += OnScrollDragging;
            scrollHandle.dragEnded += OnScrollDragEnded;
            m_PolyUI.scrollHandle.hoverStarted += OnScrollHoverStarted;
            m_PolyUI.scrollHandle.hoverEnded += OnScrollHoverEnded;

            // Propagate initial bounds
            OnBoundsChanged();
        }

        void SetupCategoryFilterUI()
        {
            m_CategoryFilterUI = EditorXRUtils.Instantiate(m_FilterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
            m_CategoryFilterUI.transform.localPosition += Vector3.right * k_FilterUIWidth * 3;
            foreach (var mb in m_CategoryFilterUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            m_CategoryFilterUI.filterChanged += () =>
            {
                var gridView = m_PolyUI.gridView;
                var searchQuery = m_CategoryFilterUI.searchQuery;
                if (string.IsNullOrEmpty(searchQuery))
                    gridView.category = PolyCategory.UNSPECIFIED;
                else
                    gridView.category = (PolyCategory)Enum.Parse(typeof(PolyCategory), searchQuery.ToUpper());

                gridView.RequestAssetList();
                UpdateComplexityFilterUI();
            };

            m_CategoryFilterUI.buttonClicked += handle =>
            {
                m_SortingUI.SetListVisibility(false);
                m_FormatFilterUI.SetListVisibility(false);
                m_ComplexityFilterUI.SetListVisibility(false);
            };

            var categoryList = new List<string>();
            var textInfo = Thread.CurrentThread.CurrentCulture.TextInfo;

            foreach (var category in Enum.GetNames(typeof(PolyCategory)))
            {
                if (category == "UNSPECIFIED")
                    continue;

                categoryList.Add(textInfo.ToTitleCase(category.ToLower()));
            }

            m_CategoryFilterUI.filterList = categoryList;

            m_CategoryFilterUI.GetComponentInChildren<Tooltip>().tooltipText = "Filter by Category";

            UpdateCategoryFilterUI();
        }

        void UpdateCategoryFilterUI()
        {
            var searchQuery = m_CategoryFilterUI.searchQuery;
            if (string.IsNullOrEmpty(searchQuery))
            {
                m_CategoryFilterUI.summaryText.text = "All";
                m_CategoryFilterUI.descriptionText.text = "Showing all categories";
            }
            else
            {
                m_CategoryFilterUI.summaryText.text = searchQuery;
                m_CategoryFilterUI.descriptionText.text = "Showing " + searchQuery;
            }
        }

        void SetupComplextyFilterUI()
        {
            m_ComplexityFilterUI = EditorXRUtils.Instantiate(m_FilterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
            m_ComplexityFilterUI.transform.localPosition += Vector3.right * k_FilterUIWidth * 2;
            foreach (var mb in m_ComplexityFilterUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            m_ComplexityFilterUI.filterChanged += () =>
            {
                var gridView = m_PolyUI.gridView;
                switch (m_ComplexityFilterUI.searchQuery)
                {
                    case k_Medium:
                        gridView.complexity = PolyMaxComplexityFilter.MEDIUM;
                        break;
                    case k_Simple:
                        gridView.complexity = PolyMaxComplexityFilter.SIMPLE;
                        break;
                    default:
                        gridView.complexity = PolyMaxComplexityFilter.UNSPECIFIED;
                        break;
                }

                gridView.RequestAssetList();
                UpdateComplexityFilterUI();
            };

            m_ComplexityFilterUI.buttonClicked += handle =>
            {
                m_SortingUI.SetListVisibility(false);
                m_FormatFilterUI.SetListVisibility(false);
                m_CategoryFilterUI.SetListVisibility(false);
            };

            m_ComplexityFilterUI.filterList = new List<string>
            {
                k_Medium,
                k_Simple
            };

            m_ComplexityFilterUI.GetComponentInChildren<Tooltip>().tooltipText = "Filter by Complexity";

            UpdateComplexityFilterUI();
        }

        void UpdateComplexityFilterUI()
        {
            switch (m_ComplexityFilterUI.searchQuery)
            {
                case k_Medium:
                    m_ComplexityFilterUI.summaryText.text = k_Featured;
                    m_ComplexityFilterUI.descriptionText.text = "Showing simple and medium models";
                    break;
                case k_Simple:
                    m_ComplexityFilterUI.summaryText.text = k_Simple;
                    m_ComplexityFilterUI.descriptionText.text = "Showing simple models";
                    break;
                default:
                    m_ComplexityFilterUI.summaryText.text = "All";
                    m_ComplexityFilterUI.descriptionText.text = "Showing all complexities";
                    break;
            }
        }

        void SetupFormatFilterUI()
        {
            m_FormatFilterUI = EditorXRUtils.Instantiate(m_FilterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
            m_FormatFilterUI.transform.localPosition += Vector3.right * k_FilterUIWidth;
            foreach (var mb in m_FormatFilterUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            m_FormatFilterUI.filterChanged += () =>
            {
                var gridView = m_PolyUI.gridView;
                switch (m_FormatFilterUI.searchQuery)
                {
                    case k_Blocks:
                        gridView.format = PolyFormatFilter.BLOCKS;
                        break;
                    case k_TiltBrush:
                        gridView.format = PolyFormatFilter.TILT;
                        break;
                    default:
                        gridView.format = null;
                        break;
                }

                gridView.RequestAssetList();
                UpdateFormatFilterUI();
            };

            m_FormatFilterUI.buttonClicked += handle =>
            {
                m_SortingUI.SetListVisibility(false);
                m_ComplexityFilterUI.SetListVisibility(false);
                m_CategoryFilterUI.SetListVisibility(false);
            };

            m_FormatFilterUI.filterList = new List<string>
            {
                k_Blocks,
                k_TiltBrush
            };

            m_FormatFilterUI.GetComponentInChildren<Tooltip>().tooltipText = "Filter by Format";

            UpdateFormatFilterUI();
        }

        void UpdateFormatFilterUI()
        {
            switch (m_FormatFilterUI.searchQuery)
            {
                case k_Blocks:
                    m_FormatFilterUI.summaryText.text = k_Blocks;
                    m_FormatFilterUI.descriptionText.text = "Showing Blocks models";
                    break;
                case k_TiltBrush:
                    m_FormatFilterUI.summaryText.text = k_TiltBrush;
                    m_FormatFilterUI.descriptionText.text = "Showing Tiltbrush models";
                    break;
                default:
                    m_FormatFilterUI.summaryText.text = "All";
                    m_FormatFilterUI.descriptionText.text = "Showing all formats";
                    break;
            }
        }

        void SetupSortingUI()
        {
            m_SortingUI = EditorXRUtils.Instantiate(m_FilterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
            foreach (var mb in m_SortingUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            m_SortingUI.filterChanged += () =>
            {
                var gridView = m_PolyUI.gridView;
                switch (m_SortingUI.searchQuery)
                {
                    case k_Featured:
                        gridView.sorting = PolyOrderBy.BEST;
                        break;
                    case k_Newest:
                        gridView.sorting = PolyOrderBy.NEWEST;
                        break;
                }

                gridView.RequestAssetList();
                UpdateSortingUI();
            };

            m_SortingUI.buttonClicked += handle =>
            {
                m_FormatFilterUI.SetListVisibility(false);
                m_ComplexityFilterUI.SetListVisibility(false);
                m_CategoryFilterUI.SetListVisibility(false);
            };

            m_SortingUI.addDefaultOption = false;
            m_SortingUI.filterList = new List<string>
            {
                k_Featured,
                k_Newest
            };

            m_SortingUI.GetComponentInChildren<Tooltip>().tooltipText = "Change sorting";

            UpdateSortingUI();
        }

        void UpdateSortingUI()
        {
            switch (m_SortingUI.searchQuery)
            {
                case k_Featured:
                    m_SortingUI.summaryText.text = k_Featured;
                    m_SortingUI.descriptionText.text = "Sorted by popularity";
                    break;
                case k_Newest:
                    m_SortingUI.summaryText.text = k_Newest;
                    m_SortingUI.descriptionText.text = "Sorted by date";
                    break;
            }
        }

        public object OnSerializeWorkspace()
        {
            var preferences = new Preferences();
            preferences.scaleFactor = m_PolyUI.gridView.scaleFactor;
            return preferences;
        }

        public void OnDeserializeWorkspace(object obj)
        {
            var preferences = (Preferences)obj;
            m_PolyUI.gridView.scaleFactor = preferences.scaleFactor;
            UpdateZoomSliderValue();
        }

        protected override void OnBoundsChanged()
        {
            var size = contentBounds.size;
            var gridView = m_PolyUI.gridView;
            size.x -= k_DoubleFaceMargin; // Shrink the content width, so that there is space allowed to grab and scroll
            size.z -= k_DoubleFaceMargin; // Reduce the height of the inspector contents as to fit within the bounds of the workspace
            gridView.size = size;
        }

        void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = true;

            m_WorkspaceUI.topHighlight.visible = true;
            m_WorkspaceUI.amplifyTopHighlight = false;

            m_PolyUI.gridView.OnBeginScrolling();
        }

        void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_PolyUI.gridView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
        }

        void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = false;

            m_WorkspaceUI.topHighlight.visible = false;

            m_PolyUI.gridView.OnScrollEnded();
        }

        void OnScrollHoverStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            if (!m_Scrolling && m_HighlightDelayCoroutine == null)
            {
                m_HighlightDelayCoroutine = StartCoroutine(DelayHighlight());
            }
        }

        IEnumerator DelayHighlight()
        {
            yield return new WaitForSeconds(k_HighlightDelay);
            m_WorkspaceUI.topHighlight.visible = true;
            m_WorkspaceUI.amplifyTopHighlight = true;
        }

        void OnScrollHoverEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            this.StopCoroutine(ref m_HighlightDelayCoroutine);
            if (!m_Scrolling && m_WorkspaceUI.gameObject.activeInHierarchy) // Check active to prevent errors in OnDestroy
            {
                m_WorkspaceUI.topHighlight.visible = false;
                m_WorkspaceUI.amplifyTopHighlight = false;
            }
        }

        void Scale(float value)
        {
            m_PolyUI.gridView.scaleFactor = Mathf.Pow(10, value);
        }

        void UpdateZoomSliderValue()
        {
            m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(m_PolyUI.gridView.scaleFactor);
        }
    }
#else
    // Non-Workspace stub to protect serialization
    sealed class PolyWorkspace : MonoBehaviour
    {
        [SerializeField]
        Vector3 m_MinBounds;

        [SerializeField]
        GameObject m_BasePrefab;

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        HapticPulse m_ButtonClickPulse;

        [SerializeField]
        HapticPulse m_ButtonHoverPulse;

        [SerializeField]
        HapticPulse m_ResizePulse;

        [SerializeField]
        HapticPulse m_MovePulse;

        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_SliderPrefab;
    }
#endif
}
