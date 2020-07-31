using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorXR.Core;
using Unity.EditorXR.Handles;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using Unity.ListViewFramework;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Workspaces
{
    class DraggableListItem<TData, TIndex> : EditorXRListViewItem<TData, TIndex>, IUsesGetPreviewOrigin, IUsesViewerScale,
        IRayToNode where TData : IListViewItemData<TIndex>
    {
        const float k_MagnetizeDuration = 0.5f;
        protected const float k_DragDeadZone = 0.025f;

        protected Transform m_DragObject;

        protected float m_DragLerp;

        readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();
        bool m_DirectGrab;

        protected virtual bool singleClickDrag { get { return true; } }

        public Action<TIndex, Transform, bool> setRowGrabbed { protected get; set; }
        public Func<Transform, ListViewItem<TData, TIndex>> getGrabbedRow { protected get; set; }

#if !FI_AUTOFILL
        IProvidesGetPreviewOrigin IFunctionalitySubscriber<IProvidesGetPreviewOrigin>.provider { get; set; }
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        protected virtual void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            if (singleClickDrag)
            {
                m_DragObject = handle.transform;
                m_DragLerp = 0;
                StartCoroutine(Magnetize());

                if (dragStart != null)
                    dragStart(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
            }
            else
            {
                // Cache eventData.direct because it is always true while dragging
                m_DirectGrab = eventData.direct;
                m_DragObject = null;
                m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;
            }
        }

        // Smoothly interpolate grabbed object into position, instead of "popping."
        protected virtual IEnumerator Magnetize()
        {
            var startTime = Time.realtimeSinceStartup;
            var currTime = 0f;
            while (currTime < k_MagnetizeDuration)
            {
                currTime = Time.realtimeSinceStartup - startTime;
                m_DragLerp = currTime / k_MagnetizeDuration;
                yield return null;
            }

            m_DragLerp = 1;
            OnMagnetizeEnded();
        }

        protected virtual void OnDragging(BaseHandle handle, HandleEventData eventData)
        {
            if (singleClickDrag)
            {
                if (m_DragObject)
                {
                    var previewOrigin = this.GetPreviewOriginForRayOrigin(eventData.rayOrigin);
                    MathUtilsExt.LerpTransform(m_DragObject, previewOrigin.position, previewOrigin.rotation, m_DragLerp);

                    if (dragging != null)
                        dragging(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
                }
            }
            else
            {
                // Only allow direct grabbing
                if (!m_DirectGrab)
                    return;

                var rayOrigin = eventData.rayOrigin;
                var directDragStart = m_DragStarts[rayOrigin];
                var dragVector = rayOrigin.position - directDragStart;
                var distance = dragVector.magnitude;

                if (m_DragObject == null && distance > k_DragDeadZone * this.GetViewerScale())
                {
                    m_DragObject = handle.transform;
                    OnDragStarted(handle, eventData, directDragStart);
                }

                if (m_DragObject)
                    OnDragging(handle, eventData, directDragStart);
            }
        }

        protected virtual void OnDragStarted(BaseHandle handle, HandleEventData eventData, Vector3 dragStartPosition)
        {
            if (dragStart != null)
                dragStart(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnDragging(BaseHandle handle, HandleEventData eventData, Vector3 dragStartPosition)
        {
            if (dragging != null)
                dragging(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            m_DragObject = null;

            if (dragEnd != null)
                dragEnd(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnHoverStart(BaseHandle handle, HandleEventData eventData)
        {
            if (hoverStart != null)
                hoverStart(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnHoverEnd(BaseHandle handle, HandleEventData eventData)
        {
            if (hoverEnd != null)
                hoverEnd(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnClick(BaseHandle handle, HandleEventData eventData)
        {
            if (click != null)
                click(this.RequestNodeFromRayOrigin(eventData.rayOrigin));
        }

        protected virtual void OnMagnetizeEnded() {}
    }
}
