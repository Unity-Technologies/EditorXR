using System.Collections.Generic;
using Unity.EditorXR.Data;
using Unity.EditorXR.Handles;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Workspaces
{
#if UNITY_EDITOR
    [MainMenuItem("Inspector", "Workspaces", "View and edit GameObject properties")]
    sealed class InspectorWorkspace : Workspace, ISelectionChanged, IInspectorWorkspace
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_LockPrefab;
#pragma warning restore 649

        InspectorUI m_InspectorUI;
        GameObject m_SelectedObject;
        LockUI m_LockUI;

        bool m_Scrolling;

        bool m_IsLocked;

        public override void Setup()
        {
            // Initial bounds must be set before the base.Setup() is called
            minBounds = new Vector3(0.502f, MinBounds.y, 0.3f);
            m_CustomStartingBounds = new Vector3(0.502f, MinBounds.y, 0.6f);

            base.Setup();
            var content = EditorXRUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
            m_InspectorUI = content.GetComponent<InspectorUI>();
            foreach (var behavior in content.GetComponentsInChildren<MonoBehaviour>(true))
            {
                this.InjectFunctionalitySingle(behavior);
            }

            m_LockUI = EditorXRUtils.Instantiate(m_LockPrefab, m_WorkspaceUI.frontPanel, false).GetComponentInChildren<LockUI>();
            this.ConnectInterfaces(m_LockUI);
            m_LockUI.clicked += OnLockButtonClicked;
            m_LockUI.hovered += OnButtonHovered;
            EditorApplication.delayCall += m_LockUI.Setup; // Need to write stencilRef after WorkspaceButton does it

            var listView = m_InspectorUI.listView;
            this.ConnectInterfaces(listView);
            listView.data = new List<InspectorData>();

            var scrollHandle = m_InspectorUI.scrollHandle;
            scrollHandle.dragStarted += OnScrollDragStarted;
            scrollHandle.dragging += OnScrollDragging;
            scrollHandle.dragEnded += OnScrollDragEnded;
            scrollHandle.hoverStarted += OnScrollHoverStarted;
            scrollHandle.hoverEnded += OnScrollHoverEnded;

            contentBounds = new Bounds(Vector3.zero, m_CustomStartingBounds.Value);

            var scrollHandleTransform = m_InspectorUI.scrollHandle.transform;
            scrollHandleTransform.SetParent(m_WorkspaceUI.topFaceContainer);
            scrollHandleTransform.localScale = new Vector3(1.03f, 0.02f, 1.02f); // Extra space for scrolling
            scrollHandleTransform.localPosition = new Vector3(0f, -0.01f, 0f); // Offset from content for collision purposes

#if UNITY_EDITOR
            listView.arraySizeChanged += OnArraySizeChanged;

            if (Selection.activeGameObject)
                OnSelectionChanged();

            UnityEditor.Undo.postprocessModifications += OnPostprocessModifications;
            UnityEditor.Undo.undoRedoPerformed += UpdateCurrentObject;
#endif

            // Propagate initial bounds
            OnBoundsChanged();
        }

        void UpdateCurrentObject()
        {
            UpdateCurrentObject(true);
        }

        void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = true;

            m_WorkspaceUI.topHighlight.visible = true;
            m_WorkspaceUI.amplifyTopHighlight = false;

            m_InspectorUI.listView.OnScrollStarted();
        }

        void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_InspectorUI.listView.scrollOffset += Vector3.Dot(eventData.deltaPosition, handle.transform.forward) / this.GetViewerScale();
        }

        void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
        {
            m_Scrolling = false;

            m_WorkspaceUI.topHighlight.visible = false;

            m_InspectorUI.listView.OnScrollEnded();
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
            if (m_IsLocked)
                return;

            if (Selection.activeGameObject == m_SelectedObject)
                return;

            if (Selection.activeGameObject == null)
            {
                m_InspectorUI.listView.data = new List<InspectorData>();
                m_SelectedObject = null;
                return;
            }

            m_SelectedObject = Selection.activeGameObject;
            UpdateInspectorData(m_SelectedObject, true);
        }

        void UpdateInspectorData(GameObject selection, bool fullReload)
        {
            var listView = m_InspectorUI.listView;
            if (fullReload)
            {
                var inspectorData = new List<InspectorData>();

#if UNITY_EDITOR
                var objectChildren = new List<InspectorData>();
                foreach (var component in selection.GetComponents<Component>())
                {
                    var obj = new SerializedObject(component);

                    var componentChildren = new List<InspectorData>();

                    var property = obj.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.depth == 0)
                            componentChildren.Add(SerializedPropertyToPropertyData(property, obj));
                    }

                    var componentData = new InspectorData("InspectorComponentItem", obj, componentChildren);
                    objectChildren.Add(componentData);
                }

                var objectData = new InspectorData("InspectorHeaderItem", new SerializedObject(selection), objectChildren);
                inspectorData.Add(objectData);
#else
                // TODO: Runtime serialization
#endif

                listView.data = inspectorData;
            }
            else
            {
                listView.OnObjectModified();
            }
        }

        void UpdateCurrentObject(bool fullReload)
        {
            if (m_SelectedObject)
                UpdateInspectorData(m_SelectedObject, fullReload);
        }

#if UNITY_EDITOR
        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!m_SelectedObject || !IncludesCurrentObject(modifications))
                return modifications;

            UpdateCurrentObject(false);

            return modifications;
        }

        bool IncludesCurrentObject(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
            {
                if (modification.previousValue.target == m_SelectedObject)
                    return true;

                if (modification.currentValue.target == m_SelectedObject)
                    return true;

                foreach (var component in m_SelectedObject.GetComponents<Component>())
                {
                    if (modification.previousValue.target == component)
                        return true;

                    if (modification.currentValue.target == component)
                        return true;
                }
            }

            return false;
        }

        PropertyData SerializedPropertyToPropertyData(SerializedProperty property, SerializedObject obj)
        {
            string template;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Quaternion:
                    template = "InspectorVectorItem";
                    break;
                case SerializedPropertyType.Integer:
                    goto case SerializedPropertyType.Float;
                case SerializedPropertyType.Float:
                    template = "InspectorNumberItem";
                    break;
                case SerializedPropertyType.Character:
                case SerializedPropertyType.String:
                    template = "InspectorStringItem";
                    break;
                case SerializedPropertyType.Bounds:
                    template = "InspectorBoundsItem";
                    break;
                case SerializedPropertyType.Boolean:
                    template = "InspectorBoolItem";
                    break;
                case SerializedPropertyType.ObjectReference:
                    template = "InspectorObjectFieldItem";
                    break;
                case SerializedPropertyType.Color:
                    template = "InspectorColorItem";
                    break;
                case SerializedPropertyType.Rect:
                    template = "InspectorRectItem";
                    break;
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                    template = "InspectorDropDownItem";
                    break;
                case SerializedPropertyType.Generic:
                    return GenericProperty(property, obj);
                default:
                    template = "InspectorUnimplementedItem";
                    break;
            }

            return new PropertyData(template, obj, null, property.Copy());
        }

        PropertyData GenericProperty(SerializedProperty property, SerializedObject obj)
        {
            var children = GetSubProperties(property, obj);

            var propertyData = property.isArray
                ? new PropertyData("InspectorArrayHeaderItem", obj, children, property.Copy())
                : new PropertyData("InspectorGenericItem", obj, children, property.Copy());

            propertyData.childrenChanging += m_InspectorUI.listView.OnBeforeChildrenChanged;

            return propertyData;
        }

        List<InspectorData> GetSubProperties(SerializedProperty property, SerializedObject obj)
        {
            var children = new List<InspectorData>();
            var iteratorProperty = property.Copy();
            while (iteratorProperty.NextVisible(true))
            {
                if (iteratorProperty.depth == 0)
                    break;

                switch (iteratorProperty.propertyType)
                {
                    case SerializedPropertyType.ArraySize:
                        children.Add(new PropertyData("InspectorNumberItem", obj, null, iteratorProperty.Copy()));
                        break;
                    default:
                        children.Add(SerializedPropertyToPropertyData(iteratorProperty, obj));
                        break;
                }
            }

            return children;
        }

        void OnArraySizeChanged(List<InspectorData> data, PropertyData element)
        {
            foreach (var d in data)
            {
                if (FindElementAndUpdateParent(d, element))
                    break;
            }
        }

        bool FindElementAndUpdateParent(InspectorData parent, PropertyData element)
        {
            if (parent.children != null)
            {
                foreach (var child in parent.children)
                {
                    if (child == element)
                    {
                        var propertyData = (PropertyData)parent;
                        propertyData.children = GetSubProperties(propertyData.property.Copy(), propertyData.serializedObject);
                        return true;
                    }

                    if (FindElementAndUpdateParent(child, element))
                        return true;
                }
            }

            return false;
        }
#endif

        protected override void OnBoundsChanged()
        {
            var size = contentBounds.size;
            var listView = m_InspectorUI.listView;
            var bounds = contentBounds;
            size.y = float.MaxValue; // Add height for dropdowns
            size.x -= k_DoubleFaceMargin; // Shrink the content width, so that there is space allowed to grab and scroll
            size.z -= FaceMargin; // Reduce the height of the inspector contents as to fit within the bounds of the workspace
            bounds.size = size;
            listView.size = bounds.size;

            var listPanel = m_InspectorUI.listPanel;
            listPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            listPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.z);
        }

        void SetIsLocked()
        {
            m_IsLocked = !m_IsLocked;
            m_LockUI.UpdateIcon(m_IsLocked);

            if (!m_IsLocked)
                OnSelectionChanged();

            OnButtonClicked(null);
        }

#if UNITY_EDITOR
        protected override void OnDestroy()
        {
            UnityEditor.Undo.postprocessModifications -= OnPostprocessModifications;
            UnityEditor.Undo.undoRedoPerformed -= UpdateCurrentObject;
            EditorApplication.hierarchyChanged -= UpdateCurrentObject;
            base.OnDestroy();
        }
#endif

        void OnLockButtonClicked(Transform rayOrigin)
        {
            SetIsLocked();
            OnButtonClicked(rayOrigin);
        }

        public void UpdateInspector(GameObject obj, bool fullRebuild = false)
        {
            if (obj == null || obj == m_SelectedObject)
                UpdateCurrentObject(fullRebuild);
        }
    }
#else
    [EditorOnlyWorkspace]
    sealed class InspectorWorkspace : Workspace
    {
        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_LockPrefab;
    }
#endif
}
