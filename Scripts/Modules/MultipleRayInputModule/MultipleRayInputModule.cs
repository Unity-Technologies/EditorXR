using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Proxies;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	// Based in part on code provided by VREAL at https://github.com/VREALITY/ViveUGUIModule/, which is licensed under the MIT License
	public class MultipleRayInputModule : BaseInputModule, IProcessInput
	{
		public class RaycastSource
		{
			public IProxy proxy; // Needed for checking if proxy is active
			public Transform rayOrigin;
			public Node node;
			public UIActions actionMapInput;
			public RayEventData eventData;
			public GameObject hoveredObject;
			public GameObject draggedObject;
			public Func<RaycastSource, bool> isValid;

			public GameObject currentObject { get { return hoveredObject ? hoveredObject : draggedObject; } }

			public bool hasObject { get { return currentObject != null && (s_LayerMask & (1 << currentObject.layer)) != 0; } }

			public RaycastSource(IProxy proxy, Transform rayOrigin, Node node, UIActions actionMapInput, Func<RaycastSource, bool> validationCallback)
			{
				this.proxy = proxy;
				this.rayOrigin = rayOrigin;
				this.node = node;
				this.actionMapInput = actionMapInput;
				this.isValid = validationCallback ?? delegate { return true; };
			}
		}

		private readonly Dictionary<Transform, RaycastSource> m_RaycastSources = new Dictionary<Transform, RaycastSource>();

		public Camera eventCamera { get { return m_EventCamera; } set { m_EventCamera = value; } }
		private Camera m_EventCamera;

		public LayerMask layerMask { get { return s_LayerMask; } set { s_LayerMask = value; } }
		private static LayerMask s_LayerMask;

		public ActionMap actionMap { get { return m_UIActionMap; } }
		[SerializeField]
		private ActionMap m_UIActionMap;

		public Func<Transform, float> getPointerLength { get; set; }

		public event Action<GameObject, RayEventData> rayEntered = delegate {};
		public event Action<GameObject, RayEventData> rayExited = delegate {};
		public event Action<GameObject, RayEventData> dragStarted = delegate {};
		public event Action<GameObject, RayEventData> dragEnded = delegate {};

		public Action<Transform> preProcessRaycastSource;

		protected override void Awake()
		{
			base.Awake();
			s_LayerMask = LayerMask.GetMask("UI");
		}

		public void AddRaycastSource(IProxy proxy, Node node, ActionMapInput actionMapInput, Transform rayOrigin, Func<RaycastSource, bool> validationCallback = null)
		{
			UIActions actions = (UIActions)actionMapInput;
			actions.active = false;
			m_RaycastSources.Add(rayOrigin, new RaycastSource(proxy, rayOrigin, node, actions, validationCallback));
		}

		public void RemoveRaycastSource(Transform rayOrigin)
		{
			m_RaycastSources.Remove(rayOrigin);
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
			// We don't process with all other input modules because we need fine-grained control to consume input
		}

		public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
		{
			ExecuteUpdateOnSelectedObject();

			if (m_EventCamera == null)
				return;

			//Process events for all different transforms in RayOrigins
			var sources = new List<RaycastSource>(m_RaycastSources.Values); // The sources dictionary can change during iteration, so cache it before iterating
			foreach (var source in sources)
			{
				if (!(source.rayOrigin.gameObject.activeSelf || source.draggedObject) || !source.proxy.active)
					continue;

				if (preProcessRaycastSource != null)
					preProcessRaycastSource(source.rayOrigin);

				if (source.eventData == null)
					source.eventData = new RayEventData(base.eventSystem);
				source.hoveredObject = GetRayIntersection(source); // Check all currently running raycasters

				var eventData = source.eventData;
				eventData.node = source.node;
				eventData.rayOrigin = source.rayOrigin;
				eventData.pointerLength = getPointerLength(eventData.rayOrigin);

				if (!source.isValid(source))
					continue;

				HandlePointerExitAndEnter(eventData, source.hoveredObject); // Send enter and exit events

				var hasScrollHandler = false;
				source.actionMapInput.active = source.hasObject && ShouldActivateInput(eventData, source.currentObject, out hasScrollHandler);

				// Proceed only if pointer is interacting with something
				if (!source.actionMapInput.active)
					continue;

				// Send select pressed and released events
				if (source.actionMapInput.select.wasJustPressed)
				{
					OnSelectPressed(source);
					consumeControl(source.actionMapInput.select);
				}

				if (source.actionMapInput.select.wasJustReleased)
					OnSelectReleased(source);

				var draggedObject = source.draggedObject;

				// Send Drag Events
				if (source.draggedObject != null)
				{
					ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.dragHandler);
					ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.dragHandler);
				}

				// Send scroll events
				var scrollObject = source.hoveredObject;
				if (!scrollObject)
					scrollObject = source.draggedObject;

				if (scrollObject && hasScrollHandler)
				{
					var actionMapInput = source.actionMapInput;
					var verticalScroll = actionMapInput.verticalScroll;
					var horizontalScroll = actionMapInput.horizontalScroll;
					var verticalScrollValue = verticalScroll.value;
					var horizontalScrollValue = horizontalScroll.value;
					if (!Mathf.Approximately(verticalScrollValue, 0f) || !Mathf.Approximately(horizontalScrollValue, 0f))
					{
						consumeControl(verticalScroll);
						consumeControl(horizontalScroll);
						eventData.scrollDelta = new Vector2(horizontalScrollValue, verticalScrollValue);
						ExecuteEvents.ExecuteHierarchy(scrollObject, eventData, ExecuteEvents.scrollHandler);
					}
				}
			}
		}

		static bool ShouldActivateInput(RayEventData eventData, GameObject currentObject, out bool hasScrollHandler)
		{
			hasScrollHandler = false;

			var selectionFlags = currentObject.GetComponent<ISelectionFlags>();
			if (selectionFlags != null && selectionFlags.selectionFlags == SelectionFlags.Direct && !U.UI.IsDirectEvent(eventData))
				return false;

			hasScrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(currentObject);

			return ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IPointerDownHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IPointerUpHandler>(currentObject)

				|| ExecuteEvents.GetEventHandler<IDragHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IBeginDragHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IEndDragHandler>(currentObject)

				|| ExecuteEvents.GetEventHandler<IRayDragHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IRayBeginDragHandler>(currentObject)
				|| ExecuteEvents.GetEventHandler<IRayEndDragHandler>(currentObject)

				|| hasScrollHandler;
		}

		private RayEventData CloneEventData(RayEventData eventData)
		{
			RayEventData clone = new RayEventData(base.eventSystem);
			clone.rayOrigin = eventData.rayOrigin;
			clone.node = eventData.node;
			clone.hovered = new List<GameObject>(eventData.hovered);
			clone.pointerEnter = eventData.pointerEnter;
			clone.pointerCurrentRaycast = eventData.pointerCurrentRaycast;
			clone.pointerLength = eventData.pointerLength;

			return clone;
		}

		protected void HandlePointerExitAndEnter(RayEventData eventData, GameObject newEnterTarget)
		{
			// Cache properties before executing base method, so we can complete additional ray events later
			var cachedEventData = CloneEventData(eventData);

			// This will modify the event data (new target will be set)
			base.HandlePointerExitAndEnter(eventData, newEnterTarget);

			if (newEnterTarget == null || cachedEventData.pointerEnter == null)
			{
				for (var i = 0; i < cachedEventData.hovered.Count; ++i)
				{
					ExecuteEvents.Execute(cachedEventData.hovered[i], eventData, ExecuteRayEvents.rayExitHandler);
					rayExited(cachedEventData.hovered[i], eventData);
				}

				if (newEnterTarget == null)
					return;
			}

			Transform t = null;

			// if we have not changed hover target
			if (cachedEventData.pointerEnter == newEnterTarget && newEnterTarget)
			{
				t = newEnterTarget.transform;
				while (t != null)
				{
					ExecuteEvents.Execute(t.gameObject, cachedEventData, ExecuteRayEvents.rayHoverHandler);
					t = t.parent;
				}
				return;
			}

			GameObject commonRoot = FindCommonRoot(cachedEventData.pointerEnter, newEnterTarget);

			// and we already an entered object from last time
			if (cachedEventData.pointerEnter != null)
			{
				// send exit handler call to all elements in the chain
				// until we reach the new target, or null!
				t = cachedEventData.pointerEnter.transform;

				while (t != null)
				{
					// if we reach the common root break out!
					if (commonRoot != null && commonRoot.transform == t)
						break;

					ExecuteEvents.Execute(t.gameObject, cachedEventData, ExecuteRayEvents.rayExitHandler);
					rayExited(t.gameObject, cachedEventData);

					t = t.parent;
				}
			}

			// now issue the enter call up to but not including the common root
			cachedEventData.pointerEnter = newEnterTarget;
			t = newEnterTarget.transform;
			while (t != null && t.gameObject != commonRoot)
			{
				ExecuteEvents.Execute(t.gameObject, cachedEventData, ExecuteRayEvents.rayEnterHandler);
				rayEntered(t.gameObject, cachedEventData);

				t = t.parent;
			}
		}

		private void OnSelectPressed(RaycastSource source)
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
				GameObject newPressed = ExecuteEvents.ExecuteHierarchy(draggedObject, eventData, ExecuteEvents.pointerDownHandler);

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
						if (U.UI.IsDoubleClick(diffTime))
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
				dragStarted(draggedObject, eventData);

				eventData.pointerDrag = draggedObject;
				source.draggedObject = draggedObject;
			}
		}

		private void OnSelectReleased(RaycastSource source)
		{
			var eventData = source.eventData;
			var hoveredObject = source.hoveredObject;

			if (source.draggedObject)
				ExecuteEvents.Execute(source.draggedObject, eventData, ExecuteEvents.pointerUpHandler);

			if (source.draggedObject)
			{
				var draggedObject = source.draggedObject;
				ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.endDragHandler);
				ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.endDragHandler);
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
			if (base.eventSystem.currentSelectedGameObject)
				base.eventSystem.SetSelectedGameObject(null);
		}

		private void Select(GameObject go)
		{
			Deselect();

			if (ExecuteEvents.GetEventHandler<ISelectHandler>(go))
				base.eventSystem.SetSelectedGameObject(go);
		}

		private GameObject GetRayIntersection(RaycastSource source)
		{
			GameObject hit = null;
			// Move camera to position and rotation for the ray origin
			m_EventCamera.transform.position = source.rayOrigin.position;
			m_EventCamera.transform.rotation = source.rayOrigin.rotation;

			RayEventData eventData = source.eventData;
			eventData.Reset();
			eventData.delta = Vector2.zero;
			eventData.position = m_EventCamera.pixelRect.center;
			eventData.scrollDelta = Vector2.zero;

			eventSystem.RaycastAll(eventData, m_RaycastResultCache);
			eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
			hit = eventData.pointerCurrentRaycast.gameObject;

			m_RaycastResultCache.Clear();
			return hit;
		}

		private bool ExecuteUpdateOnSelectedObject()
		{
			if (base.eventSystem.currentSelectedGameObject == null)
				return false;

			BaseEventData eventData = GetBaseEventData();
			ExecuteEvents.Execute(base.eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.updateSelectedHandler);
			return eventData.used;
		}
	}
}
