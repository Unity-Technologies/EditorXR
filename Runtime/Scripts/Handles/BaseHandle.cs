using System;
using System.Collections.Generic;
using Unity.EditorXR.Modules;
using Unity.EditorXR.UI;
using Unity.EditorXR.Utilities;
using Unity.XRTools.Utils.GUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.EditorXR.Handles
{
    /// <summary>
    /// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
    /// </summary>
    class BaseHandle : MonoBehaviour, ISelectionFlags, IRayBeginDragHandler, IRayDragHandler, IRayEndDragHandler,
        IRayEnterHandler, IRayExitHandler, IRayHoverHandler, IPointerClickHandler, IDropReceiver, IDroppable
    {
        protected const int k_DefaultCapacity = 2; // i.e. 2 controllers

        public SelectionFlags selectionFlags
        {
            get { return m_SelectionFlags; }
            set { m_SelectionFlags = value; }
        }

        [SerializeField]
        [FlagsProperty]
        SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

        protected readonly List<Transform> m_HoverSources = new List<Transform>(k_DefaultCapacity);
        protected readonly List<Transform> m_DragSources = new List<Transform>(k_DefaultCapacity);
        protected readonly Dictionary<Transform, Vector3> m_StartDragPositions = new Dictionary<Transform, Vector3>(k_DefaultCapacity);
        protected readonly Dictionary<Transform, DateTime> m_LastClickTimes = new Dictionary<Transform, DateTime>(k_DefaultCapacity);

        public bool hasHoverSource { get { return m_HoverSources.Count > 0; } }
        public bool hasDragSource { get { return m_DragSources.Count > 0; } }
        public Dictionary<Transform, Vector3> startDragPositions { get { return m_StartDragPositions; } }

        public Func<BaseHandle, object, bool> canDrop { private get; set; }
        public Action<BaseHandle, object> receiveDrop { private get; set; }
        public Func<BaseHandle, object> getDropObject { private get; set; }

        public event Action<BaseHandle> dropHoverStarted;
        public event Action<BaseHandle> dropHoverEnded;

        public event Action<BaseHandle, HandleEventData> dragStarted;
        public event Action<BaseHandle, HandleEventData> dragging;
        public event Action<BaseHandle, HandleEventData> dragEnded;

        public event Action<BaseHandle, PointerEventData> click;
        public event Action<BaseHandle, HandleEventData> doubleClick;

        public event Action<BaseHandle, HandleEventData> hoverStarted;
        public event Action<BaseHandle, HandleEventData> hovering;
        public event Action<BaseHandle, HandleEventData> hoverEnded;

        // Local method use only -- created here to reduce garbage collection
        static readonly HandleEventData k_HandleEventData = new HandleEventData(null, false);

        void Awake()
        {
            // Put this object in the UI layer so that it is hit by UI raycasts
            gameObject.layer = LayerMask.NameToLayer("UI");
        }

        void OnDisable()
        {
            if (m_HoverSources.Count > 0 || m_DragSources.Count > 0)
            {
                var eventData = GetHandleEventData(new RayEventData(EventSystem.current));
                var sources = new List<Transform>(m_HoverSources);
                m_HoverSources.Clear();
                foreach (var rayOrigin in sources)
                {
                    eventData.rayOrigin = rayOrigin;
                    OnHandleHoverEnded(eventData);
                }

                sources.Clear();
                sources.AddRange(m_DragSources);
                m_DragSources.Clear();
                foreach (var rayOrigin in sources)
                {
                    eventData.rayOrigin = rayOrigin;
                    OnHandleDragEnded(eventData);
                }
            }
        }

        protected virtual HandleEventData GetHandleEventData(RayEventData eventData)
        {
            k_HandleEventData.rayOrigin = eventData.rayOrigin;
            k_HandleEventData.camera = eventData.camera;
            k_HandleEventData.position = eventData.position;
            k_HandleEventData.direct = UIUtils.IsDirectEvent(eventData);
            return k_HandleEventData;
        }

        public int IndexOfHoverSource(Transform rayOrigin)
        {
            return m_HoverSources.IndexOf(rayOrigin);
        }

        public int IndexOfDragSource(Transform rayOrigin)
        {
            return m_DragSources.IndexOf(rayOrigin);
        }

        public void OnBeginDrag(RayEventData eventData)
        {
            if (!UIUtils.IsValidEvent(eventData, selectionFlags))
                return;

            var rayOrigin = eventData.rayOrigin;
            m_DragSources.Add(rayOrigin);
            startDragPositions[rayOrigin] = eventData.pointerCurrentRaycast.worldPosition;

            var handleEventData = GetHandleEventData(eventData);

            //Double-click logic
            DateTime lastClickTime;
            if (!m_LastClickTimes.TryGetValue(rayOrigin, out lastClickTime))
                m_LastClickTimes[rayOrigin] = new DateTime();

            var now = DateTime.UtcNow;
            var timeSinceLastClick = (float)(now - lastClickTime).TotalSeconds;
            m_LastClickTimes[rayOrigin] = now;
            if (UIUtils.IsDoubleClick(timeSinceLastClick))
                OnDoubleClick(handleEventData);

            OnHandleDragStarted(handleEventData);
        }

        public void OnDrag(RayEventData eventData)
        {
            if (m_DragSources.Count > 0)
                OnHandleDragging(GetHandleEventData(eventData));
        }

        public void OnEndDrag(RayEventData eventData)
        {
            if (m_DragSources.Remove(eventData.rayOrigin))
                OnHandleDragEnded(GetHandleEventData(eventData));
        }

        public void OnRayEnter(RayEventData eventData)
        {
            if (!UIUtils.IsValidEvent(eventData, selectionFlags))
                return;

            m_HoverSources.Add(eventData.rayOrigin);
            OnHandleHoverStarted(GetHandleEventData(eventData));
        }

        public void OnRayHover(RayEventData eventData)
        {
            var handleEventData = GetHandleEventData(eventData);

            // Direct selection has special handling for enter/exit since those events may not have been called
            // because the pointer wasn't close enough to the handle
            if (selectionFlags == SelectionFlags.Direct)
            {
                var rayOrigin = eventData.rayOrigin;
                if (!handleEventData.direct && m_HoverSources.Remove(rayOrigin))
                {
                    OnHandleHoverEnded(handleEventData);
                    return;
                }

                if (handleEventData.direct && !m_HoverSources.Contains(rayOrigin))
                {
                    m_HoverSources.Add(rayOrigin);
                    OnHandleHoverStarted(handleEventData);
                }
            }

            if (m_HoverSources.Count > 0)
                OnHandleHovering(GetHandleEventData(eventData));
        }

        public void OnRayExit(RayEventData eventData)
        {
            if (m_HoverSources.Remove(eventData.rayOrigin))
                OnHandleHoverEnded(GetHandleEventData(eventData));
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleHoverStarted(HandleEventData eventData)
        {
            if (hoverStarted != null)
                hoverStarted(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleHovering(HandleEventData eventData)
        {
            if (hovering != null)
                hovering(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleHoverEnded(HandleEventData eventData)
        {
            if (hoverEnded != null)
                hoverEnded(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleDragStarted(HandleEventData eventData)
        {
            if (dragStarted != null)
                dragStarted(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleDragging(HandleEventData eventData)
        {
            if (dragging != null)
                dragging(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnHandleDragEnded(HandleEventData eventData)
        {
            if (dragEnded != null)
                dragEnded(this, eventData);
        }

        /// <summary>
        /// Override to modify event data prior to raising event (requires calling base method at the end)
        /// </summary>
        protected virtual void OnDoubleClick(HandleEventData eventData)
        {
            if (doubleClick != null)
                doubleClick(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (click != null)
                click(this, eventData);
        }

        public virtual bool CanDrop(object dropObject)
        {
            if (canDrop != null)
                return canDrop(this, dropObject);

            return false;
        }

        public virtual void ReceiveDrop(object dropObject)
        {
            if (receiveDrop != null)
                receiveDrop(this, dropObject);
        }

        public virtual object GetDropObject()
        {
            if (!this) // If this handle has ben destroyed, return null;
                return null;

            if (getDropObject != null)
                return getDropObject(this);

            return null;
        }

        public void OnDropHoverStarted()
        {
            if (dropHoverStarted != null)
                dropHoverStarted(this);
        }

        public void OnDropHoverEnded()
        {
            if (dropHoverEnded != null)
                dropHoverEnded(this);
        }
    }
}
