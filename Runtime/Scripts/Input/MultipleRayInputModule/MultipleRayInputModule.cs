using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    // Based in part on code provided by VREAL at https://github.com/VREALITY/ViveUGUIModule/, which is licensed under the MIT License
    class MultipleRayInputModule : RayInputModule, IUsesPointer, IUsesConnectInterfaces,
        IProvidesIsHoveringOverUI, IUsesFunctionalityInjection, IProvidesBlockUIInteraction
    {
        public class RaycastSource : ICustomActionMap, IUsesRequestFeedback, IRaycastSource
        {
            public IProxy proxy; // Needed for checking if proxy is active
            public Node node;
            public Func<IRaycastSource, bool> isValid;

            MultipleRayInputModule m_Owner;
            readonly List<ProxyFeedbackRequest> m_ScrollFeedback = new List<ProxyFeedbackRequest>();

            public GameObject hoveredObject { get; private set; }
            public GameObject draggedObject { get; set; }
            public Camera eventCamera { get { return m_Owner.m_EventCamera; } }

            public Vector2 position { get { return m_Owner.m_EventCamera.pixelRect.center; } }

            public Transform rayOrigin { get; private set; }
            public RayEventData eventData { get; private set; }
            public bool blocked { get; set; }

            public GameObject currentObject { get { return hoveredObject ? hoveredObject : draggedObject; } }

            public bool hasObject { get { return currentObject != null && (s_LayerMask & (1 << currentObject.layer)) != 0; } }

            public ActionMap actionMap { get { return MultipleRayInputModuleSettings.instance.UIActionMap; } }
            public bool ignoreActionMapInputLocking { get { return false; } }

#if !FI_AUTOFILL
            IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
#endif

            public RaycastSource(IProxy proxy, Transform rayOrigin, Node node, MultipleRayInputModule owner, Func<IRaycastSource, bool> validationCallback)
            {
                this.proxy = proxy;
                this.rayOrigin = rayOrigin;
                this.node = node;
                m_Owner = owner;
                isValid = validationCallback;
            }

            public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
            {
                if (!(rayOrigin.gameObject.activeSelf || draggedObject) || !proxy.active)
                    return;

                var preProcessRaycastSource = m_Owner.preProcessRaycastSource;
                if (preProcessRaycastSource != null)
                    preProcessRaycastSource(rayOrigin);

                if (eventData == null)
                    eventData = new RayEventData(m_Owner.eventSystem);

                hoveredObject = m_Owner.GetRayIntersection(this); // Check all currently running raycasters

                eventData.node = node;
                eventData.rayOrigin = rayOrigin;
                eventData.pointerLength = m_Owner.GetPointerLength(eventData.rayOrigin);

                var uiActions = (UIActions)input;
                var select = uiActions.select;

                if (isValid != null && !isValid(this))
                {
                    var currentRaycast = eventData.pointerCurrentRaycast;
                    currentRaycast.gameObject = null;
                    eventData.pointerCurrentRaycast = currentRaycast;
                    hoveredObject = null;
                    m_Owner.HandlePointerExitAndEnter(eventData, null, true); // Send only exit events

                    if (select.wasJustReleased)
                        m_Owner.OnSelectReleased(this);

                    HideScrollFeedback();

                    return;
                }

                m_Owner.HandlePointerExitAndEnter(eventData, hoveredObject); // Send enter and exit events

                var hasScrollHandler = false;
                var hasInteractable = hasObject && HoveringInteractable(eventData, currentObject, out hasScrollHandler);

                // Proceed only if pointer is interacting with something
                if (!hasInteractable)
                {
                    // If we have an object, the ray is blocked--input should not bleed through
                    if (hasObject && select.wasJustPressed)
                        consumeControl(select);

                    HideScrollFeedback();

                    if (select.wasJustReleased)
                        m_Owner.OnSelectReleased(this);

                    return;
                }

                // Send select pressed and released events
                if (select.wasJustPressed)
                {
                    m_Owner.OnSelectPressed(this);
                    consumeControl(select);
                }

                if (select.wasJustReleased)
                    m_Owner.OnSelectReleased(this);

                // Send Drag Events
                if (draggedObject != null)
                {
                    ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.dragHandler);
                    ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.dragHandler);
                }

                // Send scroll events
                if (currentObject && hasScrollHandler)
                {
                    var verticalScroll = uiActions.verticalScroll;
                    var horizontalScroll = uiActions.horizontalScroll;
                    var verticalScrollValue = verticalScroll.value;
                    var horizontalScrollValue = horizontalScroll.value;
                    if (!Mathf.Approximately(verticalScrollValue, 0f) || !Mathf.Approximately(horizontalScrollValue, 0f))
                    {
                        consumeControl(verticalScroll);
                        consumeControl(horizontalScroll);
                        eventData.scrollDelta = new Vector2(horizontalScrollValue, verticalScrollValue);
                        ExecuteEvents.ExecuteHierarchy(currentObject, eventData, ExecuteEvents.scrollHandler);
                    }

                    if (m_ScrollFeedback.Count == 0)
                        ShowScrollFeedback();
                }
            }

            void ShowFeedback(List<ProxyFeedbackRequest> requests, string controlName, string tooltipText = null)
            {
                if (tooltipText == null)
                    tooltipText = controlName;

                List<VRInputDevice.VRControl> ids;
                if (m_Owner.m_Controls.TryGetValue(controlName, out ids))
                {
                    foreach (var id in ids)
                    {
                        var request = this.GetFeedbackRequestObject<ProxyFeedbackRequest>(this);
                        request.node = node;
                        request.control = id;
                        request.tooltipText = tooltipText;
                        requests.Add(request);
                        this.AddFeedbackRequest(request);
                    }
                }
            }

            void ShowScrollFeedback()
            {
                ShowFeedback(m_ScrollFeedback, "VerticalScroll", "Scroll");
            }

            void HideFeedback(List<ProxyFeedbackRequest> requests)
            {
                foreach (var request in requests)
                {
                    this.RemoveFeedbackRequest(request);
                }

                requests.Clear();
            }

            void HideScrollFeedback()
            {
                HideFeedback(m_ScrollFeedback);
            }
        }

        static LayerMask s_LayerMask;

        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField]
        private string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        private string m_CancelButton = "Cancel";

        [SerializeField]
        private float m_InputActionsPerSecond = 10;

        [SerializeField]
        private float m_RepeatDelay = 0.5f;

        float m_PrevActionTime;
        Vector2 m_LastMoveVector;
        int m_ConsecutiveMoveCount;

        readonly Dictionary<Transform, IRaycastSource> m_RaycastSources = new Dictionary<Transform, IRaycastSource>();

        Camera m_EventCamera;

        RayEventData m_InputRayEvent;

        public Camera eventCamera
        {
            get { return m_EventCamera; }
            set { m_EventCamera = value; }
        }

        public LayerMask layerMask
        {
            get { return s_LayerMask; }
            set { s_LayerMask = value; }
        }

        public event Action<GameObject, RayEventData> rayEntered;
        public event Action<GameObject, RayEventData> rayHovering;
        public event Action<GameObject, RayEventData> rayExited;
        public event Action<GameObject, RayEventData> dragStarted;
        public event Action<GameObject, RayEventData> dragEnded;

        public Action<Transform> preProcessRaycastSource { private get; set; }

        readonly BindingDictionary m_Controls = new BindingDictionary();

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        RayEventData m_TempRayEvent;

        protected override void Awake()
        {
            base.Awake();
            s_LayerMask = LayerMask.GetMask("UI");
            m_TempRayEvent = new RayEventData(eventSystem);
            var uiActionMap = MultipleRayInputModuleSettings.instance.UIActionMap;
            InputUtils.GetBindingDictionaryFromActionMap(uiActionMap, m_Controls);
        }

        protected override void OnDestroy()
        {
            foreach (var source in m_RaycastSources)
            {
                this.DisconnectInterfaces(source);
            }
        }

        public void AddRaycastSource(IProxy proxy, Node node, Transform rayOrigin, Func<IRaycastSource, bool> validationCallback = null)
        {
            var source = new RaycastSource(proxy, rayOrigin, node, this, validationCallback);
            this.ConnectInterfaces(source, rayOrigin);
            this.InjectFunctionalitySingle(source);
            m_RaycastSources.Add(rayOrigin, source);
        }

        public RayEventData GetPointerEventData(Transform rayOrigin)
        {
            IRaycastSource source;
            if (m_RaycastSources.TryGetValue(rayOrigin, out source))
                return source.eventData;

            return null;
        }

        bool ShouldIgnoreEventsOnNoFocus()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isRemoteConnected)
                        return false;
#endif
                    return true;
                default:
                    return false;
            }
        }

        public override void Process()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            bool usedEvent = SendUpdateEventToSelectedObject();

            // case 1004066 - touch / mouse events should be processed before navigation events in case
            // they change the current selected gameobject and the submit button is a touch / mouse button.

            // touch needs to take precedence because of the mouse emulation layer
            if (!ProcessTouchEvents() && input.mousePresent)
                ProcessRayMouseEvent();

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();

                if (!usedEvent)
                    SendSubmitEventToSelectedObject();
            }

            if (m_EventCamera == null)
                return;

            // World scaling also scales clipping planes
            var camera = CameraUtils.GetMainCamera();
            m_EventCamera.nearClipPlane = camera.nearClipPlane;
            m_EventCamera.farClipPlane = camera.farClipPlane;
        }

        protected bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Calculate and send a move event to the current selected object.
        /// </summary>
        /// <returns>If the move event was used by the selected object.</returns>
        protected bool SendMoveEventToSelectedObject()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);

            // If direction didn't change at least 90 degrees, wait for delay before allowing consequtive event.
            if (similarDir && m_ConsecutiveMoveCount == 1)
            {
                if (time <= m_PrevActionTime + m_RepeatDelay)
                    return false;
            }
            // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
            else
            {
                if (time <= m_PrevActionTime + 1f / m_InputActionsPerSecond)
                    return false;
            }

            var axisEventData = GetAxisEventData(movement.x, movement.y, 0.6f);

            if (axisEventData.moveDir != MoveDirection.None)
            {
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, axisEventData, ExecuteEvents.moveHandler);
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return axisEventData.used;
        }

        /// <summary>
        /// Calculate and send a submit event to the current selected object.
        /// </summary>
        /// <returns>If the submit event was used by the selected object.</returns>
        protected bool SendSubmitEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            if (input.GetButtonDown(m_SubmitButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);

            if (input.GetButtonDown(m_CancelButton))
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
            return data.used;
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }
            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }
            return move;
        }

        bool ProcessTouchEvents()
        {
            for (var i = 0; i < input.touchCount; ++i)
            {
                var touch = input.GetTouch(i);

                if (touch.type == TouchType.Indirect)
                    continue;

                bool released;
                bool pressed;
                var pointer = GetTouchRayEventData(touch, out pressed, out released);

                ProcessTouchPress(pointer, pressed, released);

                if (!released)
                {
                    ProcessMove(pointer);
                    ProcessDrag(pointer);
                }
                else
                    RemoveRayData(pointer);
            }
            return input.touchCount > 0;
        }

        void ProcessTouchPress(RayEventData rayEvent, bool pressed, bool released)
        {
            var currentOverGo = rayEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                rayEvent.eligibleForClick = true;
                rayEvent.delta = Vector2.zero;
                rayEvent.dragging = false;
                rayEvent.useDragThreshold = true;
                rayEvent.pressPosition = rayEvent.position;
                rayEvent.pointerPressRaycast = rayEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, rayEvent);

                if (rayEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(rayEvent, currentOverGo);
                    rayEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, rayEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                var time = Time.unscaledTime;

                if (newPressed == rayEvent.lastPress)
                {
                    var diffTime = time - rayEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++rayEvent.clickCount;
                    else
                        rayEvent.clickCount = 1;

                    rayEvent.clickTime = time;
                }
                else
                {
                    rayEvent.clickCount = 1;
                }

                rayEvent.pointerPress = newPressed;
                rayEvent.rawPointerPress = currentOverGo;

                rayEvent.clickTime = time;

                // Save the drag handler as well
                var dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);
                if (dragHandler == null)
                    dragHandler = ExecuteEvents.GetEventHandler<IRayDragHandler>(currentOverGo);

                rayEvent.pointerDrag = dragHandler;

                if (dragHandler != null)
                    ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteEvents.initializePotentialDrag);

                m_InputRayEvent = rayEvent;
            }

            // PointerUp notification
            if (released)
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
                    ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteEvents.endDragHandler);
                    ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteRayEvents.endDragHandler);
                }

                rayEvent.dragging = false;
                rayEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(rayEvent.pointerEnter, rayEvent, ExecuteEvents.pointerExitHandler);
                rayEvent.pointerEnter = null;

                m_InputRayEvent = rayEvent;
            }
        }

        void ProcessRayMouseEvent()
        {
            var mouseData = GetMouseRayEventData();
            var leftButtonData = mouseData.GetRayButtonState(PointerEventData.InputButton.Left).eventData;

            //m_CurrentFocusedGameObject = leftButtonData.buttonData.pointerCurrentRaycast.gameObject;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetRayButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetRayButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetRayButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetRayButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        bool GetScreenRayData(int id, out RayEventData data, bool create)
        {
            if (!m_RayData.TryGetValue(id, out data) && create)
            {
                var mainCamera = CameraUtils.GetMainCamera();
                data = new RayEventData(eventSystem)
                {
                    pointerId = id,
                    rayOrigin = mainCamera.transform,
                    camera = mainCamera
                };

                m_RayData.Add(id, data);
                return true;
            }
            return false;
        }

        void ProcessMousePress(MouseButtonRayEventData data)
        {
            var rayEvent = data.buttonData;
            var currentOverGo = rayEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                rayEvent.eligibleForClick = true;
                rayEvent.delta = Vector2.zero;
                rayEvent.dragging = false;
                rayEvent.useDragThreshold = true;
                rayEvent.pressPosition = rayEvent.position;
                rayEvent.pointerPressRaycast = rayEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, rayEvent);

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, rayEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                var time = Time.unscaledTime;

                if (newPressed == rayEvent.lastPress)
                {
                    var diffTime = time - rayEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++rayEvent.clickCount;
                    else
                        rayEvent.clickCount = 1;

                    rayEvent.clickTime = time;
                }
                else
                {
                    rayEvent.clickCount = 1;
                }

                rayEvent.pointerPress = newPressed;
                rayEvent.rawPointerPress = currentOverGo;

                rayEvent.clickTime = time;

                // Save the drag handler as well
                var dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);
                if (dragHandler == null)
                    dragHandler = ExecuteEvents.GetEventHandler<IRayDragHandler>(currentOverGo);

                rayEvent.pointerDrag = dragHandler;

                if (dragHandler != null)
                    ExecuteEvents.Execute(dragHandler, rayEvent, ExecuteEvents.initializePotentialDrag);

                m_InputRayEvent = rayEvent;
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ReleaseMouse(rayEvent, currentOverGo);
            }
        }

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                if (m_InputRayEvent != null && m_InputRayEvent.pointerDrag != null && m_InputRayEvent.dragging)
                {
                    ReleaseMouse(m_InputRayEvent, m_InputRayEvent.pointerCurrentRaycast.gameObject);
                }

                m_InputRayEvent = null;
            }
        }

        void ReleaseMouse(RayEventData rayEvent, GameObject currentOverGo)
        {
            ExecuteEvents.Execute(rayEvent.pointerPress, rayEvent, ExecuteEvents.pointerUpHandler);

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
                ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteEvents.endDragHandler);
                ExecuteEvents.Execute(rayEvent.pointerDrag, rayEvent, ExecuteRayEvents.endDragHandler);
            }

            rayEvent.dragging = false;
            rayEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != rayEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(rayEvent, null);
                HandlePointerExitAndEnter(rayEvent, currentOverGo);
            }

            m_InputRayEvent = rayEvent;
        }

        static bool HoveringInteractable(RayEventData eventData, GameObject currentObject, out bool hasScrollHandler)
        {
            hasScrollHandler = false;

            var selectionFlags = ComponentUtils<ISelectionFlags>.GetComponent(currentObject);
            if (selectionFlags != null && selectionFlags.selectionFlags == SelectionFlags.Direct && !UIUtils.IsDirectEvent(eventData))
                return false;

            hasScrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(currentObject);

            return ExecuteEvents.GetEventHandler<IEventSystemHandler>(currentObject);
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

        void HandlePointerExitAndEnter(RayEventData eventData, GameObject newEnterTarget, bool exitOnly = false)
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

        void OnSelectPressed(IRaycastSource source)
        {
            Deselect();

            var eventData = source.eventData;
            var hoveredObject = source.hoveredObject;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
            eventData.pointerPress = hoveredObject;
            eventData.dragging = false;
            eventData.useDragThreshold = true;

            if (hoveredObject != null) // Pressed when pointer is over something
            {
                var newPressed = ExecuteEvents.ExecuteHierarchy(hoveredObject, eventData, ExecuteEvents.pointerDownHandler);

                if (newPressed == null) // Gameobject does not have pointerDownHandler in hierarchy, but may still have click handler
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoveredObject);

                if (newPressed != null)
                {
                    hoveredObject = newPressed; // Set current pressed to gameObject that handles the pointerDown event, not the root object
                    Select(hoveredObject);
                    eventData.eligibleForClick = true;

                    // Track clicks for double-clicking, triple-clicking, etc.
                    float time = Time.realtimeSinceStartup;
                    if (newPressed == eventData.lastPress)
                    {
                        var diffTime = time - eventData.clickTime;
                        if (UIUtils.IsDoubleClick(diffTime))
                            ++eventData.clickCount;
                        else
                            eventData.clickCount = 1;
                    }
                    else
                    {
                        eventData.clickCount = 1;
                    }

                    eventData.clickTime = time;
                }

                var draggedObject = ExecuteEvents.GetEventHandler<IDragHandler>(hoveredObject);
                if (draggedObject == null)
                    draggedObject = ExecuteEvents.GetEventHandler<IRayDragHandler>(hoveredObject);

                eventData.pointerDrag = draggedObject;
                source.draggedObject = draggedObject;

                if (eventData.pointerDrag != null)
                    ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.initializePotentialDrag);
            }
        }

        void OnSelectReleased(IRaycastSource source)
        {
            var eventData = source.eventData;
            var hoveredObject = source.hoveredObject;

            if (source.draggedObject)
                ExecuteEvents.Execute(source.draggedObject, eventData, ExecuteEvents.pointerUpHandler);

            if (source.draggedObject)
            {
                var draggedObject = source.draggedObject;
                if (dragEnded != null)
                    dragEnded(draggedObject, eventData);

                if (hoveredObject != null)
                    ExecuteEvents.ExecuteHierarchy(hoveredObject, eventData, ExecuteEvents.dropHandler);

                eventData.pointerDrag = null;
            }

            var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoveredObject);
            if (source.draggedObject == clickHandler && eventData.eligibleForClick)
                ExecuteEvents.Execute(clickHandler, eventData, ExecuteEvents.pointerClickHandler);

            eventData.dragging = false;
            eventData.rawPointerPress = null;
            eventData.pointerPress = null;
            eventData.eligibleForClick = false;
            source.draggedObject = null;
        }

        public void Deselect()
        {
            if (eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null);
        }

        void Select(GameObject go)
        {
            Deselect();

            if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
                eventSystem.SetSelectedGameObject(go);
        }

        internal GameObject GetRayIntersection(IRaycastSource source)
        {
            var eventCamera = source.eventCamera;
            if (eventCamera == null)
            {
                // Move camera to position and rotation for the ray origin
                eventCamera = m_EventCamera;
                var eventCameraTransform = eventCamera.transform;
                var sourceRayOrigin = source.rayOrigin;
                eventCameraTransform.position = sourceRayOrigin.position;
                eventCameraTransform.rotation = sourceRayOrigin.rotation;
            }

            var eventData = source.eventData;
            eventData.Reset();
            eventData.delta = Vector2.zero;
            eventData.position = source.position;
            eventData.scrollDelta = Vector2.zero;

            var ray = eventCamera.ScreenPointToRay(source.position);
            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            var hit = eventData.pointerCurrentRaycast.gameObject;

            m_RaycastResultCache.Clear();
            return hit;
        }

        public bool IsHoveringOverUI(Transform rayOrigin)
        {
            IRaycastSource source;
            return m_RaycastSources.TryGetValue(rayOrigin, out source) && source.hasObject;
        }

        public void SetUIBlockedForRayOrigin(Transform rayOrigin, bool blocked)
        {
            IRaycastSource source;
            if (m_RaycastSources.TryGetValue(rayOrigin, out source))
                source.blocked = blocked;
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var isHoveringOverUISubscriber = obj as IFunctionalitySubscriber<IProvidesIsHoveringOverUI>;
            if (isHoveringOverUISubscriber != null)
                isHoveringOverUISubscriber.provider = this;

            var blockUIInteractionSubscriber = obj as IFunctionalitySubscriber<IProvidesBlockUIInteraction>;
            if (blockUIInteractionSubscriber != null)
                blockUIInteractionSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }
    }
}
