using System;
using System.Collections.Generic;
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
    sealed class MultipleRayInputModule : BaseInputModule, IModule, IUsesPointer, IConnectInterfaces
    {
        public class RaycastSource : ICustomActionMap, IRequestFeedback
        {
            public IProxy proxy; // Needed for checking if proxy is active
            public Transform rayOrigin;
            public Node node;
            public RayEventData eventData;
            public GameObject hoveredObject;
            public GameObject draggedObject;
            public bool blocked;
            public Func<RaycastSource, bool> isValid;

            MultipleRayInputModule m_Owner;
            readonly List<ProxyFeedbackRequest> m_ScrollFeedback = new List<ProxyFeedbackRequest>();

            public GameObject currentObject
            {
                get { return hoveredObject ? hoveredObject : draggedObject; }
            }

            public bool hasObject
            {
                get { return currentObject != null && (s_LayerMask & (1 << currentObject.layer)) != 0; }
            }

            public ActionMap actionMap
            {
                get { return m_Owner.m_UIActionMap; }
            }

            public bool ignoreActionMapInputLocking
            {
                get { return false; }
            }

            public RaycastSource(IProxy proxy, Transform rayOrigin, Node node, MultipleRayInputModule owner, Func<RaycastSource, bool> validationCallback)
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
                        var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
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

#pragma warning disable 649
        [SerializeField]
        ActionMap m_UIActionMap;
#pragma warning restore 649

        readonly Dictionary<Transform, RaycastSource> m_RaycastSources = new Dictionary<Transform, RaycastSource>();

        Camera m_EventCamera;

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

        // Local method use only -- created here to reduce garbage collection
        RayEventData m_TempRayEvent;

        public void LoadModule()
        {
            s_LayerMask = LayerMask.GetMask("UI");
            m_TempRayEvent = new RayEventData(eventSystem);
            InputUtils.GetBindingDictionaryFromActionMap(m_UIActionMap, m_Controls);
        }

        public void UnloadModule()
        {
            foreach (var source in m_RaycastSources)
            {
                this.DisconnectInterfaces(source);
            }
        }

        public void AddRaycastSource(IProxy proxy, Node node, Transform rayOrigin, Func<RaycastSource, bool> validationCallback = null)
        {
            var source = new RaycastSource(proxy, rayOrigin, node, this, validationCallback);
            this.ConnectInterfaces(source, rayOrigin);
            m_RaycastSources.Add(rayOrigin, source);
        }

        public RayEventData GetPointerEventData(Transform rayOrigin)
        {
            RaycastSource source;
            if (m_RaycastSources.TryGetValue(rayOrigin, out source))
                return source.eventData;

            return null;
        }

        public override void Process()
        {
            ExecuteUpdateOnSelectedObject();

            if (m_EventCamera == null)
                return;

            // World scaling also scales clipping planes
            var camera = CameraUtils.GetMainCamera();
            m_EventCamera.nearClipPlane = camera.nearClipPlane;
            m_EventCamera.farClipPlane = camera.farClipPlane;
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
            clone.node = eventData.node;
            clone.hovered.Clear();
            clone.hovered.AddRange(eventData.hovered);
            clone.pointerEnter = eventData.pointerEnter;
            clone.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
            clone.pointerLength = eventData.pointerLength;

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
                        ExecuteEvents.Execute(transform.gameObject, cachedEventData, ExecuteRayEvents.rayHoverHandler);
                        if (rayHovering != null)
                            rayHovering(transform.gameObject, cachedEventData);

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

                    ExecuteEvents.Execute(transform.gameObject, cachedEventData, ExecuteRayEvents.rayExitHandler);
                    if (rayExited != null)
                        rayExited(transform.gameObject, cachedEventData);

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
                    ExecuteEvents.Execute(transform.gameObject, cachedEventData, ExecuteRayEvents.rayEnterHandler);
                    if (rayEntered != null)
                        rayEntered(transform.gameObject, cachedEventData);

                    transform = transform.parent;
                }
            }
        }

        void OnSelectPressed(RaycastSource source)
        {
            Deselect();

            var eventData = source.eventData;
            var hoveredObject = source.hoveredObject;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
            eventData.pointerPress = hoveredObject;

            if (hoveredObject != null) // Pressed when pointer is over something
            {
                var draggedObject = hoveredObject;
                var newPressed = ExecuteEvents.ExecuteHierarchy(draggedObject, eventData, ExecuteEvents.pointerDownHandler);

                if (newPressed == null) // Gameobject does not have pointerDownHandler in hierarchy, but may still have click handler
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(draggedObject);

                if (newPressed != null)
                {
                    draggedObject = newPressed; // Set current pressed to gameObject that handles the pointerDown event, not the root object
                    Select(draggedObject);
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

                ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.beginDragHandler);
                eventData.dragging = true;
                if (dragStarted != null)
                    dragStarted(draggedObject, eventData);

                eventData.pointerDrag = draggedObject;
                source.draggedObject = draggedObject;
            }
        }

        void OnSelectReleased(RaycastSource source)
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

                ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.endDragHandler);
                ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.endDragHandler);

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

        GameObject GetRayIntersection(RaycastSource source)
        {
            // Move camera to position and rotation for the ray origin
            m_EventCamera.transform.position = source.rayOrigin.position;
            m_EventCamera.transform.rotation = source.rayOrigin.rotation;

            var eventData = source.eventData;
            eventData.Reset();
            eventData.delta = Vector2.zero;
            eventData.position = m_EventCamera.pixelRect.center;
            eventData.scrollDelta = Vector2.zero;

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            var hit = eventData.pointerCurrentRaycast.gameObject;

            m_RaycastResultCache.Clear();
            return hit;
        }

        bool ExecuteUpdateOnSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var eventData = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.updateSelectedHandler);
            return eventData.used;
        }

        public bool IsHoveringOverUI(Transform rayOrigin)
        {
            RaycastSource source;
            return m_RaycastSources.TryGetValue(rayOrigin, out source) && source.hasObject;
        }

        public void SetUIBlockedForRayOrigin(Transform rayOrigin, bool blocked)
        {
            RaycastSource source;
            if (m_RaycastSources.TryGetValue(rayOrigin, out source))
                source.blocked = blocked;
        }
    }
}
