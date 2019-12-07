using Unity.Labs.EditorXR.Handles;
using Unity.Labs.EditorXR.Helpers;
using UnityEngine;

namespace Unity.Labs.EditorXR.Workspaces
{
#if UNITY_EDITOR
    abstract class EditorWindowWorkspace : Workspace
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_CaptureWindowPrefab;
#pragma warning restore 649

        Transform m_CaptureWindow;

        EditorWindowCapture m_Capture;

        public override void Setup()
        {
            // Initial bounds must be set before the base.Setup() is called
            minBounds = new Vector3(0.727f, MinBounds.y, 0.4f);
            m_CustomStartingBounds = minBounds;

            base.Setup();

            preventResize = true;

            m_CaptureWindow = this.InstantiateUI(m_CaptureWindowPrefab).transform;
            m_CaptureWindow.SetParent(m_WorkspaceUI.topFaceContainer, false);
            m_CaptureWindow.localPosition = new Vector3(0f, -0.007f, -0.5f);
            m_CaptureWindow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            m_CaptureWindow.localScale = new Vector3(1f, 1f, 1f);

            var bounds = contentBounds;
            var size = bounds.size;
            size.z = 0.1f;
            bounds.size = size;
            contentBounds = bounds;

            var handle = m_CaptureWindow.GetComponent<BaseHandle>();
            handle.hovering += OnHovering;
            handle.dragStarted += OnDragStarted;
            handle.dragging += OnDragging;
            handle.dragEnded += OnDragEnded;

            m_Capture = m_CaptureWindow.GetComponent<EditorWindowCapture>();
        }

        void OnHovering(BaseHandle handle, HandleEventData eventData)
        {
            m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseMove);
        }

        void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseDown);
        }

        void OnDragging(BaseHandle handle, HandleEventData eventData)
        {
            m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseDrag);
        }

        void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            m_Capture.SendEvent(eventData.rayOrigin, transform, EventType.MouseUp);
        }
    }
#else
    abstract class EditorWindowWorkspace : Workspace
    {
        [SerializeField]
        GameObject m_CaptureWindowPrefab;
    }
#endif
}
