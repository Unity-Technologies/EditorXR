#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    [MainMenuItem("Hierarchy", "Workspaces", "View all GameObjects in your scene(s)")]
    [SpatialMenuItem("Hierarchy", "Workspaces", "View all GameObjects in your scene(s)")]
    class HierarchyWorkspace : Workspace, IFilterUI, IUsesHierarchyData, ISelectionChanged, IMoveCameraRig
    {
        protected const string k_Locked = "Locked";

        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_FilterPrefab;

        [SerializeField]
        GameObject m_FocusPrefab;

        [SerializeField]
        GameObject m_CreateEmptyPrefab;

        HierarchyUI m_HierarchyUI;
        protected FilterUI m_FilterUI;

        HierarchyData m_SelectedRow;

        bool m_Scrolling;

        public List<HierarchyData> hierarchyData
        {
            set
            {
                m_HierarchyData = value;

                if (m_HierarchyUI)
                    m_HierarchyUI.listView.data = value;
            }
        }

        protected List<HierarchyData> m_HierarchyData;

        public virtual List<string> filterList
        {
            set
            {
                m_FilterList = value;
                m_FilterList.Sort();
                m_FilterList.Insert(0, k_Locked);

                if (m_FilterUI)
                    m_FilterUI.filterList = value;
            }
        }

        protected List<string> m_FilterList;

        public virtual string searchQuery { get { return m_FilterUI.searchQuery; } }

        public override void Setup()
        {
            // Initial bounds must be set before the base.Setup() is called
            minBounds = new Vector3(0.522f, MinBounds.y, 0.5f);
            m_CustomStartingBounds = minBounds;

            base.Setup();

            var contentPrefab = ObjectUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
            m_HierarchyUI = contentPrefab.GetComponent<HierarchyUI>();
            m_HierarchyUI.listView.lockedQueryString = k_Locked;
            hierarchyData = m_HierarchyData;

            if (m_FilterPrefab)
            {
                m_FilterUI = ObjectUtils.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();
                foreach (var mb in m_FilterUI.GetComponentsInChildren<MonoBehaviour>())
                {
                    this.ConnectInterfaces(mb);
                }
                m_FilterUI.filterList = m_FilterList;
            }

            if (m_FocusPrefab)
            {
                var focusUI = ObjectUtils.Instantiate(m_FocusPrefab, m_WorkspaceUI.frontPanel, false);
                foreach (var mb in focusUI.GetComponentsInChildren<MonoBehaviour>())
                {
                    this.ConnectInterfaces(mb);
                }
                var button = focusUI.GetComponentInChildren<WorkspaceButton>(true);
                button.clicked += FocusSelection;
                button.hovered += OnButtonHovered;
            }

            if (m_CreateEmptyPrefab)
            {
                var createEmptyUI = ObjectUtils.Instantiate(m_CreateEmptyPrefab, m_WorkspaceUI.frontPanel, false);
                foreach (var mb in createEmptyUI.GetComponentsInChildren<MonoBehaviour>())
                {
                    this.ConnectInterfaces(mb);
                }
                var button = createEmptyUI.GetComponentInChildren<WorkspaceButton>(true);
                button.clicked += CreateEmptyGameObject;
                button.clicked += OnButtonClicked;
                button.hovered += OnButtonHovered;
            }

            var listView = m_HierarchyUI.listView;
            listView.selectRow = SelectRow;
            listView.matchesFilter = this.MatchesFilter;
            listView.getSearchQuery = () => searchQuery;
            this.ConnectInterfaces(listView);

            var scrollHandle = m_HierarchyUI.scrollHandle;
            scrollHandle.dragStarted += OnScrollDragStarted;
            scrollHandle.dragging += OnScrollDragging;
            scrollHandle.dragEnded += OnScrollDragEnded;
            scrollHandle.hoverStarted += OnScrollHoverStarted;
            scrollHandle.hoverEnded += OnScrollHoverEnded;

            contentBounds = new Bounds(Vector3.zero, m_CustomStartingBounds.Value);

            var scrollHandleTransform = m_HierarchyUI.scrollHandle.transform;
            scrollHandleTransform.SetParent(m_WorkspaceUI.topFaceContainer);
            scrollHandleTransform.localScale = new Vector3(1.03f, 0.02f, 1.02f); // Extra space for scrolling
            scrollHandleTransform.localPosition = new Vector3(0f, -0.015f, 0f); // Offset from content for collision purposes

            m_FilterUI.buttonClicked += OnButtonClicked;
            m_FilterUI.buttonHovered += OnButtonHovered;

            // Propagate initial bounds
            OnBoundsChanged();
        }

        protected override void OnDestroy()
        {
            m_FilterUI.buttonClicked -= OnButtonClicked;
            m_FilterUI.buttonHovered -= OnButtonHovered;

            base.OnDestroy();
        }

        protected override void OnBoundsChanged()
        {
            var size = contentBounds.size;
            var listView = m_HierarchyUI.listView;
            size.y = float.MaxValue; // Add height for dropdowns
            size.x -= DoubleFaceMargin; // Shrink the content width, so that there is space allowed to grab and scroll
            size.z -= DoubleFaceMargin; // Reduce the height of the inspector contents as to fit within the bounds of the workspace
            listView.size = size;
        }

        static void SelectRow(int index)
        {
#if UNITY_EDITOR
            var gameObject = EditorUtility.InstanceIDToObject(index) as GameObject;
            if (gameObject && Selection.activeGameObject != gameObject)
                Selection.activeGameObject = gameObject;
            else
                Selection.activeGameObject = null;
#else
            //TODO: Object indices in play mode
#endif
        }

        void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = true;

            m_WorkspaceUI.topHighlight.visible = true;
            m_WorkspaceUI.amplifyTopHighlight = false;

            m_HierarchyUI.listView.OnBeginScrolling();
        }

        void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_HierarchyUI.listView.scrollOffset -= Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
        }

        void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = false;

            m_WorkspaceUI.topHighlight.visible = false;

            m_HierarchyUI.listView.OnScrollEnded();
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
            if (!m_Scrolling)
            {
                m_WorkspaceUI.topHighlight.visible = false;
                m_WorkspaceUI.amplifyTopHighlight = false;
            }
        }

        public void OnSelectionChanged()
        {
            m_HierarchyUI.listView.SelectRow(Selection.activeInstanceID);
        }

        void FocusSelection(Transform rayOrigin)
        {
            if (Selection.gameObjects.Length == 0)
                return;

            var mainCamera = CameraUtils.GetMainCamera().transform;
            var bounds = ObjectUtils.GetBounds(Selection.transforms);

            var size = bounds.size;
            size.y = 0;
            var maxSize = size.MaxComponent();

            const float kExtraDistance = 0.25f; // Add some extra distance so selection isn't in your face
            maxSize += kExtraDistance;

            var viewDirection = mainCamera.transform.forward;
            viewDirection.y = 0;
            viewDirection.Normalize();

            var cameraDiff = mainCamera.position - CameraUtils.GetCameraRig().position;
            cameraDiff.y = 0;

            this.MoveCameraRig(bounds.center - cameraDiff - viewDirection * maxSize);

            OnButtonClicked(rayOrigin);
        }

        static void CreateEmptyGameObject(Transform rayOrigin)
        {
            var camera = CameraUtils.GetMainCamera().transform;
            var go = new GameObject();
            go.transform.position = camera.position + camera.forward;
            Selection.activeGameObject = go;
        }
    }
}
#endif
