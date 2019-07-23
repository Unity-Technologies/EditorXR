using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;

namespace UnityEngine.EventSystems
{
    /// <summary>
    /// A BaseInputModule for ray-based input, based on PointerInputModule
    /// </summary>
    abstract class RayInputModule : BaseInputModule
    {
        /// <summary>
        /// Id of the cached left mouse pointer event.
        /// </summary>
        public const int kMouseLeftId = -1;

        /// <summary>
        /// Id of the cached right mouse pointer event.
        /// </summary>
        public const int kMouseRightId = -2;

        /// <summary>
        /// Id of the cached middle mouse pointer event.
        /// </summary>
        public const int kMouseMiddleId = -3;

        /// <summary>
        /// Touch id for when simulating touches on a non touch device.
        /// </summary>
        public const int kFakeTouchesId = -4;

        protected Dictionary<int, RayEventData> m_RayData = new Dictionary<int, RayEventData>();

        public event Action<GameObject, RayEventData> rayEntered;
        public event Action<GameObject, RayEventData> rayHovering;
        public event Action<GameObject, RayEventData> rayExited;
        public event Action<GameObject, RayEventData> dragStarted;
        public event Action<GameObject, RayEventData> dragEnded;

        // Local method use only -- created here to reduce garbage collection
        RayEventData m_TempRayEvent;

        protected override void Awake()
        {
            base.Awake();
            m_TempRayEvent = new RayEventData(eventSystem);
        }

        /// <summary>
        /// Search the cache for currently active pointers, return true if found.
        /// </summary>
        /// <param name="id">Touch ID</param>
        /// <param name="data">Found data</param>
        /// <param name="create">If not found should it be created</param>
        /// <returns>True if pointer is found.</returns>
        protected bool GetRayData(int id, out RayEventData data, bool create)
        {
            if (!m_RayData.TryGetValue(id, out data) && create)
            {
                var mainCamera = CameraUtils.GetMainCamera();
                data = new RayEventData(eventSystem)
                {
                    pointerId = id,
                    camera = mainCamera,
                    rayOrigin = mainCamera.transform
                };
                m_RayData.Add(id, data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove the RayEventData from the cache.
        /// </summary>
        protected void RemoveRayData(RayEventData data)
        {
            m_RayData.Remove(data.pointerId);
        }

        /// <summary>
        /// Given a touch populate the RayEventData and return if we are pressed or released.
        /// </summary>
        /// <param name="input">Touch being processed</param>
        /// <param name="pressed">Are we pressed this frame</param>
        /// <param name="released">Are we released this frame</param>
        /// <returns></returns>
        protected RayEventData GetTouchRayEventData(Touch input, out bool pressed, out bool released)
        {
            RayEventData pointerData;
            var created = GetRayData(input.fingerId, out pointerData, true);

            pointerData.Reset();

            pressed = created || (input.phase == TouchPhase.Began);
            released = (input.phase == TouchPhase.Canceled) || (input.phase == TouchPhase.Ended);

            if (created)
                pointerData.position = input.position;

            if (pressed)
                pointerData.delta = Vector2.zero;
            else
                pointerData.delta = input.position - pointerData.position;

            pointerData.position = input.position;

            pointerData.button = PointerEventData.InputButton.Left;

            if (input.phase == TouchPhase.Canceled)
            {
                pointerData.pointerCurrentRaycast = new RaycastResult();
            }
            else
            {
                eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

                var raycast = FindFirstRaycast(m_RaycastResultCache);
                pointerData.pointerCurrentRaycast = raycast;
                m_RaycastResultCache.Clear();
            }
            return pointerData;
        }

        /// <summary>
        /// Copy one RayEventData to another.
        /// </summary>
        protected void CopyFromTo(RayEventData @from, RayEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
        }

        /// <summary>
        /// Given a mouse button return the current state for the frame.
        /// </summary>
        /// <param name="buttonId">Mouse button ID</param>
        protected PointerEventData.FramePressState StateForMouseButton(int buttonId)
        {
            var pressed = input.GetMouseButtonDown(buttonId);
            var released = input.GetMouseButtonUp(buttonId);
            if (pressed && released)
                return PointerEventData.FramePressState.PressedAndReleased;
            if (pressed)
                return PointerEventData.FramePressState.Pressed;
            if (released)
                return PointerEventData.FramePressState.Released;
            return PointerEventData.FramePressState.NotChanged;
        }

        protected class RayButtonState
        {
            private PointerEventData.InputButton m_Button = PointerEventData.InputButton.Left;

            public MouseButtonRayEventData eventData
            {
                get { return m_EventData; }
                set { m_EventData = value; }
            }

            public PointerEventData.InputButton button
            {
                get { return m_Button; }
                set { m_Button = value; }
            }

            private MouseButtonRayEventData m_EventData;
        }

        protected class RayMouseState
        {
            private List<RayButtonState> m_TrackedButtons = new List<RayButtonState>();

            public bool AnyPressesThisFrame()
            {
                for (int i = 0; i < m_TrackedButtons.Count; i++)
                {
                    if (m_TrackedButtons[i].eventData.PressedThisFrame())
                        return true;
                }
                return false;
            }

            public bool AnyReleasesThisFrame()
            {
                for (int i = 0; i < m_TrackedButtons.Count; i++)
                {
                    if (m_TrackedButtons[i].eventData.ReleasedThisFrame())
                        return true;
                }
                return false;
            }

            public RayButtonState GetButtonState(PointerEventData.InputButton button)
            {
                RayButtonState tracked = null;
                for (int i = 0; i < m_TrackedButtons.Count; i++)
                {
                    if (m_TrackedButtons[i].button == button)
                    {
                        tracked = m_TrackedButtons[i];
                        break;
                    }
                }

                if (tracked == null)
                {
                    tracked = new RayButtonState { button = button, eventData = new MouseButtonRayEventData() };
                    m_TrackedButtons.Add(tracked);
                }
                return tracked;
            }

            public void SetButtonState(PointerEventData.InputButton button, PointerEventData.FramePressState stateForMouseButton, RayEventData data)
            {
                var toModify = GetButtonState(button);
                toModify.eventData.buttonState = stateForMouseButton;
                toModify.eventData.buttonData = data;
            }
        }

        /// <summary>
        /// Information about a mouse button event.
        /// </summary>
        protected class MouseButtonRayEventData
        {
            /// <summary>
            /// The state of the button this frame.
            /// </summary>
            public PointerEventData.FramePressState buttonState;

            /// <summary>
            /// Pointer data associated with the mouse event.
            /// </summary>
            public RayEventData buttonData;

            /// <summary>
            /// Was the button pressed this frame?
            /// </summary>
            public bool PressedThisFrame()
            {
                return buttonState == PointerEventData.FramePressState.Pressed || buttonState == PointerEventData.FramePressState.PressedAndReleased;
            }

            /// <summary>
            /// Was the button released this frame?
            /// </summary>
            public bool ReleasedThisFrame()
            {
                return buttonState == PointerEventData.FramePressState.Released || buttonState == PointerEventData.FramePressState.PressedAndReleased;
            }
        }

        private readonly RayMouseState m_MouseState = new RayMouseState();

        /// <summary>
        /// Return the current MouseState. Using the default pointer.
        /// </summary>
        protected virtual RayMouseState GetMouseRayEventData()
        {
            return GetMouseRayEventData(0);
        }

        /// <summary>
        /// Return the current MouseState.
        /// </summary>
        protected virtual RayMouseState GetMouseRayEventData(int id)
        {
            // Populate the left button...
            RayEventData leftData;
            var created = GetRayData(kMouseLeftId, out leftData, true);

            leftData.Reset();

            if (created)
                leftData.position = input.mousePosition;

            var pos = input.mousePosition;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // We don't want to do ANY cursor-based interaction when the mouse is locked
                leftData.position = new Vector2(-1.0f, -1.0f);
                leftData.delta = Vector2.zero;
            }
            else
            {
                leftData.delta = pos - leftData.position;
                leftData.position = pos;
            }

            leftData.scrollDelta = input.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

            // copy the appropriate data into right and middle slots
            RayEventData rightData;
            GetRayData(kMouseRightId, out rightData, true);
            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;

            RayEventData middleData;
            GetRayData(kMouseMiddleId, out middleData, true);
            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            m_MouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

            return m_MouseState;
        }

        /// <summary>
        /// Return the last RayEventData for the given touch / mouse id.
        /// </summary>
        protected RayEventData GetLastRayEventData(int id)
        {
            RayEventData data;
            GetRayData(id, out data, false);
            return data;
        }

        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        /// <summary>
        /// Process movement for the current frame with the given pointer event.
        /// </summary>
        protected virtual void ProcessMove(RayEventData pointerEvent)
        {
            var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        RayEventData GetTempEventDataClone(RayEventData eventData)
        {
            var clone = m_TempRayEvent;
            clone.rayOrigin = eventData.rayOrigin;
            clone.camera = eventData.camera;
            clone.position = eventData.position;
            clone.delta = eventData.delta;
            clone.node = eventData.node;
            clone.hovered.Clear();
            clone.hovered.AddRange(eventData.hovered);
            clone.pointerEnter = eventData.pointerEnter;
            clone.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
            clone.pointerLength = eventData.pointerLength;
            clone.useDragThreshold = eventData.useDragThreshold;

            return clone;
        }

        protected void HandlePointerExitAndEnter(RayEventData eventData, GameObject newEnterTarget, bool exitOnly = false)
        {
            // Cache properties before executing base method, so we can complete additional ray events later
            var cachedEventData = GetTempEventDataClone(eventData);

            // This will modify the event data (new target will be set)
            base.HandlePointerExitAndEnter(eventData, newEnterTarget);

            var pointerEnter = cachedEventData.pointerEnter;
            if (newEnterTarget == null || pointerEnter == null)
            {
                for (var i = 0; i < cachedEventData.hovered.Count; ++i)
                {
                    var hovered = cachedEventData.hovered[i];

                    ExecuteEvents.Execute(hovered, eventData, ExecuteEvents.pointerExitHandler);
                    ExecuteEvents.Execute(hovered, eventData, ExecuteRayEvents.rayExitHandler);
                    if (rayExited != null)
                        rayExited(hovered, eventData);
                }

                if (newEnterTarget == null)
                    return;
            }

            if (!exitOnly)
            {
                // if we have not changed hover target
                if (newEnterTarget && pointerEnter == newEnterTarget)
                {
                    var transform = newEnterTarget.transform;
                    while (transform != null)
                    {
                        var hovered = transform.gameObject;
                        ExecuteEvents.Execute(hovered, cachedEventData, ExecuteRayEvents.rayHoverHandler);
                        if (rayHovering != null)
                            rayHovering(hovered, cachedEventData);

                        transform = transform.parent;
                    }

                    return;
                }
            }

            var commonRoot = FindCommonRoot(pointerEnter, newEnterTarget);

            // and we already an entered object from last time
            if (pointerEnter != null)
            {
                // send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                var transform = pointerEnter.transform;

                while (transform != null)
                {
                    // if we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == transform)
                        break;

                    var hovered = transform.gameObject;
                    ExecuteEvents.Execute(hovered, cachedEventData, ExecuteEvents.pointerExitHandler);
                    ExecuteEvents.Execute(hovered, cachedEventData, ExecuteRayEvents.rayExitHandler);
                    if (rayExited != null)
                        rayExited(hovered, cachedEventData);

                    transform = transform.parent;
                }
            }

            if (!exitOnly)
            {
                // now issue the enter call up to but not including the common root
                cachedEventData.pointerEnter = newEnterTarget;
                var transform = newEnterTarget.transform;
                while (transform != null && transform.gameObject != commonRoot)
                {
                    var hovered = transform.gameObject;
                    ExecuteEvents.Execute(hovered, cachedEventData, ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(hovered, cachedEventData, ExecuteRayEvents.rayEnterHandler);
                    if (rayEntered != null)
                        rayEntered(hovered, cachedEventData);

                    transform = transform.parent;
                }
            }
        }

        /// <summary>
        /// Process the drag for the current frame with the given pointer event.
        /// </summary>
        protected virtual void ProcessDrag(RayEventData rayEvent)
        {
            var draggedObject = rayEvent.pointerDrag;
            if (Cursor.lockState == CursorLockMode.Locked ||
                draggedObject == null)
                return;

            if (!rayEvent.dragging
                && ShouldStartDrag(rayEvent.pressPosition, rayEvent.position, eventSystem.pixelDragThreshold, rayEvent.useDragThreshold))
            {
                if (dragStarted != null)
                    dragStarted(draggedObject, rayEvent);

                ExecuteEvents.Execute(draggedObject, rayEvent, ExecuteRayEvents.beginDragHandler);
                ExecuteEvents.Execute(draggedObject, rayEvent, ExecuteEvents.beginDragHandler);
                rayEvent.dragging = true;
            }

            // Drag notification
            if (rayEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (rayEvent.pointerPress != draggedObject)
                {
                    ExecuteEvents.Execute(rayEvent.pointerPress, rayEvent, ExecuteEvents.pointerUpHandler);

                    rayEvent.eligibleForClick = false;
                    rayEvent.pointerPress = null;
                    rayEvent.rawPointerPress = null;
                }

                ExecuteEvents.Execute(draggedObject, rayEvent, ExecuteRayEvents.dragHandler);
                ExecuteEvents.Execute(draggedObject, rayEvent, ExecuteEvents.dragHandler);
            }
        }

        protected void OnPointerUp(RayEventData rayEvent, GameObject currentOverGo)
        {
            ExecuteEvents.Execute(rayEvent.pointerPress, rayEvent, ExecuteEvents.pointerUpHandler);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (rayEvent.pointerPress == pointerUpHandler && rayEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(rayEvent.pointerPress, rayEvent, ExecuteEvents.pointerClickHandler);
            }
            else if (rayEvent.pointerDrag != null && rayEvent.dragging)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, rayEvent, ExecuteEvents.dropHandler);
            }

            rayEvent.eligibleForClick = false;
            rayEvent.pointerPress = null;
            rayEvent.rawPointerPress = null;

            if (rayEvent.pointerDrag != null && rayEvent.dragging)
            {
                if (dragEnded != null)
                    dragEnded(currentOverGo, rayEvent);

                ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteRayEvents.endDragHandler);
                ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteEvents.endDragHandler);
            }

            rayEvent.dragging = false;
            rayEvent.pointerDrag = null;
        }

        public override bool IsPointerOverGameObject(int pointerId)
        {
            var lastPointer = GetLastRayEventData(pointerId);
            if (lastPointer != null)
                return lastPointer.pointerEnter != null;
            return false;
        }

        /// <summary>
        /// Clear all pointers and deselect any selected objects in the EventSystem.
        /// </summary>
        protected void ClearSelection()
        {
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_RayData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_RayData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("<b>Pointer Input Module of type: </b>" + GetType());
            sb.AppendLine();
            foreach (var pointer in m_RayData)
            {
                if (pointer.Value == null)
                    continue;
                sb.AppendLine("<B>Pointer:</b> " + pointer.Key);
                sb.AppendLine(pointer.Value.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deselect the current selected GameObject if the currently pointed-at GameObject is different.
        /// </summary>
        /// <param name="currentOverGo">The GameObject the pointer is currently over.</param>
        /// <param name="pointerEvent">Current event data.</param>
        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }
    }
}
