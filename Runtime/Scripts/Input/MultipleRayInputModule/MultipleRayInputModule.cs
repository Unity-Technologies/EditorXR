using System;
using System.Collections.Generic;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Proxies;
using Unity.EditorXR.Utilities;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// A BaseInputModule designed for mouse / keyboard / controller input.
    /// </summary>
    /// <remarks>
    /// Input module for working with, mouse, keyboard, or controller.
    /// </remarks>
    [AddComponentMenu("Event/Multiple Ray Input Module")]
    class MultipleRayInputModule : RayInputModule, IUsesPointer, IUsesConnectInterfaces, IProvidesAddRaycastSource,
        IProvidesIsHoveringOverUI, IUsesFunctionalityInjection, IProvidesBlockUIInteraction, IProvidesUIEvents,
        IProvidesGetRayEventData
    {
        class RaycastSource : ICustomActionMap, IUsesRequestFeedback, IRaycastSource
        {
            readonly IProxy m_Proxy; // Needed for checking if proxy is active
            readonly Node m_Node;
            readonly Func<IRaycastSource, bool> m_IsValid;
            readonly Transform m_RayOrigin;

            MultipleRayInputModule m_Owner;
            readonly List<ProxyFeedbackRequest> m_ScrollFeedback = new List<ProxyFeedbackRequest>();

            public Transform rayOrigin { get { return m_RayOrigin; } }
            public RayEventData eventData { get; private set; }
            public bool blocked { get; set; }

            public GameObject currentObject
            {
                get
                {
                    var hoveredObject = eventData.pointerCurrentRaycast.gameObject;
                    return hoveredObject ? hoveredObject : eventData.pointerDrag;
                }
            }

            public bool hasObject { get { return currentObject != null && (m_Owner.m_LayerMask & (1 << currentObject.layer)) != 0; } }

            public ActionMap actionMap { get { return MultipleRayInputModuleSettings.instance.UIActionMap; } }
            public bool ignoreActionMapInputLocking { get { return false; } }

#if !FI_AUTOFILL
            IProvidesRequestFeedback IFunctionalitySubscriber<IProvidesRequestFeedback>.provider { get; set; }
#endif

            public RaycastSource(IProxy proxy, Transform rayOrigin, Node node, MultipleRayInputModule owner, Func<IRaycastSource, bool> validationCallback)
            {
                m_Proxy = proxy;
                m_RayOrigin = rayOrigin;
                m_Node = node;
                m_Owner = owner;
                m_IsValid = validationCallback;
            }

            public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
            {
                if (!(m_RayOrigin.gameObject.activeSelf || eventData.pointerDrag) || !m_Proxy.active)
                    return;

                var preProcessRaycastSource = m_Owner.preProcessRaycastSource;
                if (preProcessRaycastSource != null)
                    preProcessRaycastSource(m_RayOrigin);

                if (eventData == null)
                    eventData = new RayEventData(m_Owner.eventSystem);

                m_Owner.GetRayIntersection(this); // Check all currently running raycasters
                var currentRaycast = eventData.pointerCurrentRaycast;
                m_Owner.m_CurrentFocusedGameObject = currentRaycast.gameObject;
                eventData.node = m_Node;
                eventData.rayOrigin = m_RayOrigin;
                eventData.pointerLength = m_Owner.GetPointerLength(m_RayOrigin);

                var uiActions = (UIActions)input;
                var select = uiActions.select;

                if (m_IsValid != null && !m_IsValid(this))
                {
                    currentRaycast.gameObject = null;
                    eventData.pointerCurrentRaycast = currentRaycast;
                    m_Owner.HandlePointerExitAndEnter(eventData, null, true); // Send only exit events

                    if (select.wasJustReleased)
                        m_Owner.OnSelectReleased(this);

                    HideScrollFeedback();

                    return;
                }

                if (currentRaycast.gameObject)
                {
                    if (select.wasJustPressed)
                    {
                        m_Owner.OnSelectPressed(this);
                        consumeControl(select);
                    }
                }

                if (select.wasJustReleased)
                    m_Owner.OnSelectReleased(this);

                m_Owner.ProcessMove(eventData);
                m_Owner.ProcessDrag(eventData, true);

                // Send scroll events
                if (currentObject)
                {
                    var hasScrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(currentObject);
                    if (hasScrollHandler)
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
                        request.node = m_Node;
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

        [SerializeField]
        string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the vertical axis for movement (if axis events are used).
        /// </summary>
        [SerializeField]
        string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the submit button.
        /// </summary>
        [SerializeField]
        string m_CancelButton = "Cancel";

        [SerializeField]
        float m_InputActionsPerSecond = 10;

        [SerializeField]
        float m_RepeatDelay = 0.5f;

        [SerializeField]
        bool m_ForceModuleActive;

        float m_PrevActionTime;
        Vector2 m_LastMoveVector;
        int m_ConsecutiveMoveCount;

        Vector2 m_LastMousePosition;
        Vector2 m_MousePosition;

        GameObject m_CurrentFocusedGameObject;

        RayEventData m_InputPointerEvent;

        LayerMask m_LayerMask;

        Camera m_MainCamera;
        Camera m_EventCamera;

        readonly Dictionary<Transform, IRaycastSource> m_RaycastSources = new Dictionary<Transform, IRaycastSource>();
        readonly BindingDictionary m_Controls = new BindingDictionary();

        public Camera eventCamera
        {
            set
            {
                m_EventCamera = value;
                m_EventCamera.fieldOfView = m_MainCamera.fieldOfView;
            }
        }

        /// <summary>
        /// Force this module to be active.
        /// </summary>
        /// <remarks>
        /// If there is no module active with higher priority (ordered in the inspector) this module will be forced active even if valid enabling conditions are not met.
        /// </remarks>
        public bool forceModuleActive
        {
            get { return m_ForceModuleActive; }
            set { m_ForceModuleActive = value; }
        }

        /// <summary>
        /// Number of keyboard / controller inputs allowed per second.
        /// </summary>
        public float inputActionsPerSecond
        {
            get { return m_InputActionsPerSecond; }
            set { m_InputActionsPerSecond = value; }
        }

        public LayerMask layerMask
        {
            get { return m_LayerMask; }
            set { m_LayerMask = value; }
        }

        public Action<Transform> preProcessRaycastSource { private get; set; }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
        IProvidesConnectInterfaces IFunctionalitySubscriber<IProvidesConnectInterfaces>.provider { get; set; }
#endif

        protected override void Awake()
        {
            base.Awake();
            m_MainCamera = CameraUtils.GetMainCamera();
            m_LayerMask = LayerMask.GetMask("UI");
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

        void OnSelectPressed(IRaycastSource source)
        {
            var eventData = source.eventData;
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            BeginPointerDown(eventData, currentOverGo);
            EndPointerDown(eventData, currentOverGo);
        }

        void OnSelectReleased(IRaycastSource source)
        {
            var eventData = source.eventData;
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            OnPointerUp(eventData, currentOverGo);
        }

        void GetRayIntersection(IRaycastSource source)
        {
            // Move camera to position and rotation for the ray origin
            var eventCameraTransform = m_EventCamera.transform;
            eventCameraTransform.position = source.rayOrigin.position;
            eventCameraTransform.rotation = source.rayOrigin.rotation;

            var eventData = source.eventData;
            eventData.Reset();
            eventData.delta = Vector2.zero;
            eventData.position = m_EventCamera.pixelRect.center;
            eventData.scrollDelta = Vector2.zero;

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            var result = FindFirstRaycast(m_RaycastResultCache);
            // TODO: Investigate zero'ed world positions
            if (result.worldPosition == Vector3.zero)
            {
                foreach (var otherResult in m_RaycastResultCache)
                {
                    if (otherResult.worldPosition != Vector3.zero)
                    {
                        result.worldPosition = otherResult.worldPosition;
                        break;
                    }
                }
            }

            eventData.pointerCurrentRaycast = result;

            m_RaycastResultCache.Clear();
        }

        static bool ShouldIgnoreEventsOnNoFocus()
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

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            {
                if (m_InputPointerEvent != null && m_InputPointerEvent.pointerDrag != null && m_InputPointerEvent.dragging)
                {
                    ReleaseMouse(m_InputPointerEvent, m_InputPointerEvent.pointerCurrentRaycast.gameObject);
                }

                m_InputPointerEvent = null;

                return;
            }

            m_LastMousePosition = m_MousePosition;
            m_MousePosition = input.mousePosition;
        }

        void ReleaseMouse(RayEventData pointerEvent, GameObject currentOverGo)
        {
            OnPointerUp(pointerEvent, currentOverGo);

            // redo pointer enter / exit to refresh state
            // so that if we moused over something that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }

            m_InputPointerEvent = pointerEvent;
        }

        public override bool IsModuleSupported()
        {
            return m_ForceModuleActive || input.mousePresent || input.touchSupported;
        }

        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;

            var shouldActivate = m_ForceModuleActive;
            shouldActivate |= input.GetButtonDown(m_SubmitButton);
            shouldActivate |= input.GetButtonDown(m_CancelButton);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f);
            shouldActivate |= !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis), 0.0f);
            shouldActivate |= (m_MousePosition - m_LastMousePosition).sqrMagnitude > 0.0f;
            shouldActivate |= input.GetMouseButtonDown(0);

            if (input.touchCount > 0)
                shouldActivate = true;

            if (m_RaycastSources.Count > 0)
                shouldActivate = true;

            return shouldActivate;
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void ActivateModule()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            base.ActivateModule();
            m_MousePosition = input.mousePosition;
            m_LastMousePosition = input.mousePosition;

            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// See BaseInputModule.
        /// </summary>
        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        public override void Process()
        {
            if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                return;

            var usedEvent = SendUpdateEventToSelectedObject();

            // case 1004066 - touch / mouse events should be processed before navigation events in case
            // they change the current selected gameobject and the submit button is a touch / mouse button.

            // touch needs to take precedence because of the mouse emulation layer
            if (!ProcessTouchEvents() && input.mousePresent)
                ProcessMouseEvent();

            if (eventSystem.sendNavigationEvents)
            {
                if (!usedEvent)
                    usedEvent |= SendMoveEventToSelectedObject();

                if (!usedEvent)
                    SendSubmitEventToSelectedObject();
            }
        }

        bool ProcessTouchEvents()
        {
            // Position the event camera to cast physics rays
            var eventCameraTransform = m_EventCamera.transform;
            var mainCameraTransform = m_MainCamera.transform;
            eventCameraTransform.position = mainCameraTransform.position;
            eventCameraTransform.rotation = mainCameraTransform.rotation;

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

        /// <summary>
        /// This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.
        /// </summary>
        /// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
        /// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
        /// <param name="released">This is true only for the last frame of a touch event.</param>
        /// <remarks>
        /// This method can be overridden in derived classes to change how touch press events are handled.
        /// </remarks>
        protected void ProcessTouchPress(RayEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                BeginPointerDown(pointerEvent, currentOverGo);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                EndPointerDown(pointerEvent, currentOverGo);
            }

            // PointerUp notification
            if (released)
            {
                OnPointerUp(pointerEvent, currentOverGo);

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;

                m_InputPointerEvent = pointerEvent;
            }
        }

        void EndPointerDown(RayEventData rayEvent, GameObject currentOverGo)
        {
            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, rayEvent, ExecuteRayEvents.pointerDownHandler);

            if (newPressed == null)
                newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, rayEvent, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IRayClickHandler>(currentOverGo);

            var time = Time.unscaledTime;

            if (newPressed == rayEvent.lastPress)
            {
                var diffTime = time - rayEvent.clickTime;
                if (diffTime < 0.3f)
                    ++rayEvent.clickCount;
                else
                    rayEvent.clickCount = 1;
            }
            else
            {
                rayEvent.clickCount = 1;
            }

            rayEvent.pointerPress = newPressed;
            rayEvent.rawPointerPress = currentOverGo;

            rayEvent.clickTime = time;

            // Save the drag handler as well
            var draggedObject = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);
            if (draggedObject == null)
                draggedObject = ExecuteEvents.GetEventHandler<IRayDragHandler>(currentOverGo);

            rayEvent.pointerDrag = draggedObject;

            if (rayEvent.pointerDrag != null)
                ExecuteEvents.Execute(draggedObject, rayEvent, ExecuteEvents.initializePotentialDrag);

            m_InputPointerEvent = rayEvent;
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

        Vector2 GetRawMoveVector()
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

        protected void ProcessMouseEvent()
        {
            ProcessMouseEvent(0);
        }

        /// <summary>
        /// Process all mouse events.
        /// </summary>
        protected void ProcessMouseEvent(int id)
        {
            // Position the event camera to cast physics rays
            var eventCameraTransform = m_EventCamera.transform;
            var mainCameraTransform = m_MainCamera.transform;
            eventCameraTransform.position = mainCameraTransform.position;
            eventCameraTransform.rotation = mainCameraTransform.rotation;

            var mouseData = GetMouseRayEventData(id);
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            m_CurrentFocusedGameObject = leftButtonData.buttonData.pointerCurrentRaycast.gameObject;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
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
        /// Calculate and process any mouse button state changes.
        /// </summary>
        protected void ProcessMousePress(MouseButtonRayEventData data)
        {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                BeginPointerDown(pointerEvent, currentOverGo);
                EndPointerDown(pointerEvent, currentOverGo);
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ReleaseMouse(pointerEvent, currentOverGo);
            }
        }

        void BeginPointerDown(RayEventData rayEvent, GameObject currentOverGo)
        {
            rayEvent.eligibleForClick = true;
            rayEvent.delta = Vector2.zero;
            rayEvent.dragging = false;
            rayEvent.useDragThreshold = true;
            rayEvent.pointerPressRaycast = rayEvent.pointerCurrentRaycast;
            rayEvent.pressPosition = rayEvent.position;

            DeselectIfSelectionChanged(currentOverGo, rayEvent);
        }

        protected GameObject GetCurrentFocusedGameObject()
        {
            return m_CurrentFocusedGameObject;
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

            var uiEventsSubscriber = obj as IFunctionalitySubscriber<IProvidesUIEvents>;
            if (uiEventsSubscriber != null)
                uiEventsSubscriber.provider = this;

            var addRaycastSourceSubscriber = obj as IFunctionalitySubscriber<IProvidesAddRaycastSource>;
            if (addRaycastSourceSubscriber != null)
                addRaycastSourceSubscriber.provider = this;

            var getPointerEventDataSubscriber = obj as IFunctionalitySubscriber<IProvidesGetRayEventData>;
            if (getPointerEventDataSubscriber != null)
                getPointerEventDataSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public void ShutDown()
        {
            m_RaycastSources.Clear();
        }
    }
}
