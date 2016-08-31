using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.VR.Proxies;

namespace UnityEngine.VR.Modules
{
	public class MultipleRayInputModule : BaseInputModule
	{
		private readonly Dictionary<Transform, RaycastSource> m_RaycastSources = new Dictionary<Transform, RaycastSource>();

		public Func<Transform, float> getPointerLength;

		public Camera eventCamera
		{
			get { return m_EventCamera; }
			set { m_EventCamera = value; }
		}
		private Camera m_EventCamera;

		public ActionMap actionMap
		{
			get { return m_UIActionMap; }
		}

		[SerializeField]
		private ActionMap m_UIActionMap;
		private int UILayer = -1;

		protected override void Awake()
		{
			base.Awake();
			UILayer = LayerMask.NameToLayer("UI");
		}

		private class RaycastSource
		{
			public IProxy proxy; // Needed for checking if proxy is active
			public Transform rayOrigin;
			public Node node;
			public UIActions actionMapInput;
			public RayEventData eventData;
			public GameObject hoveredObject;
			public GameObject pressedObject;
			public GameObject draggedObject;


			public RaycastSource(IProxy proxy, Transform rayOrigin, Node node, UIActions actionMapInput)
			{
				this.proxy = proxy;
				this.rayOrigin = rayOrigin;
				this.node = node;
				this.actionMapInput = actionMapInput;
			}
		}

		public void AddRaycastSource(IProxy proxy, Node node, ActionMapInput actionMapInput)
		{
			UIActions actions = (UIActions) actionMapInput;
			if (actions == null)
			{
				Debug.LogError("Cannot add actionMapInput to InputModule that is not of type UIActions.");
				return;
			}
			actions.active = false;
			Transform rayOrigin = null;
			if (proxy.rayOrigins.TryGetValue(node, out rayOrigin))
			{
				m_RaycastSources.Add(rayOrigin, new RaycastSource(proxy, rayOrigin, node, actions));
			}
			else
				Debug.LogError("Failed to get ray origin transform for node " + node + " from proxy " + proxy);
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

			//Process events for all different transforms in RayOrigins
			foreach (var source in m_RaycastSources.Values)
			{				
				if (!source.proxy.active)
					continue;

				if (source.eventData == null)
					source.eventData = new RayEventData(base.eventSystem);
				source.hoveredObject = GetRayIntersection(source); // Check all currently running raycasters

				var eventData = source.eventData;
				eventData.node = source.node;
				eventData.rayOrigin = source.rayOrigin;
				eventData.pointerLength = getPointerLength(eventData.rayOrigin);

				HandlePointerExitAndEnter(eventData, source.hoveredObject); // Send enter and exit events

				// Activate actionmap input only if pointer is interacting with something
				var hoveredObject = source.hoveredObject;
				var pressedObject = source.pressedObject;
				var draggedObject = source.draggedObject;
				source.actionMapInput.active = (hoveredObject != null && hoveredObject.layer == UILayer) 
												|| pressedObject != null || draggedObject != null;

				if (!source.actionMapInput.active)
					continue;

				// Send select pressed and released events
				if (source.actionMapInput.select.wasJustPressed)
					OnSelectPressed(source);

				if (source.actionMapInput.select.wasJustReleased)
					OnSelectReleased(source);

				if (source.draggedObject != null)
				{
					ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.dragHandler);
					ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.dragHandler);
				}

				// Send scroll events
				if (source.pressedObject != null)
				{
					eventData.scrollDelta = new Vector2(0f, source.actionMapInput.verticalScroll.value);
					ExecuteEvents.ExecuteHierarchy(hoveredObject, eventData, ExecuteEvents.scrollHandler);
				}
			}
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
					ExecuteEvents.Execute(cachedEventData.hovered[i], eventData, ExecuteRayEvents.rayExitHandler);

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
					t = t.parent;
				}
			}

			// now issue the enter call up to but not including the common root
			cachedEventData.pointerEnter = newEnterTarget;
			t = newEnterTarget.transform;
			while (t != null && t.gameObject != commonRoot)
			{
				ExecuteEvents.Execute(t.gameObject, cachedEventData, ExecuteRayEvents.rayEnterHandler);
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
				source.pressedObject = hoveredObject;
				GameObject newPressed = ExecuteEvents.ExecuteHierarchy(source.pressedObject, eventData, ExecuteEvents.pointerDownHandler);

				if (newPressed == null) // Gameobject does not have pointerDownHandler in hierarchy, but may still have click handler
					newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(source.pressedObject);

				if (newPressed != null)
				{
					source.pressedObject = newPressed; // Set current pressed to gameObject that handles the pointerDown event, not the root object
					eventData.pointerPress = newPressed;
					Select(source.pressedObject);
					eventData.eligibleForClick = true;
				}
				var pressedObject = source.pressedObject;
				ExecuteEvents.Execute(pressedObject, eventData, ExecuteEvents.beginDragHandler);
				ExecuteEvents.Execute(pressedObject, eventData, ExecuteRayEvents.beginDragHandler);
				eventData.pointerDrag = pressedObject;
				source.draggedObject = pressedObject;
			}
		}

		private void OnSelectReleased(RaycastSource source)
		{
			var eventData = source.eventData;
			var hoveredObject = source.hoveredObject;
			
			if (source.pressedObject)
				ExecuteEvents.Execute(source.pressedObject, eventData, ExecuteEvents.pointerUpHandler);

			if (source.draggedObject)
			{
				var draggedObject = source.draggedObject;
				ExecuteEvents.Execute(draggedObject, eventData, ExecuteEvents.endDragHandler);
				ExecuteEvents.Execute(draggedObject, eventData, ExecuteRayEvents.endDragHandler);

				if (hoveredObject != null)
					ExecuteEvents.ExecuteHierarchy(hoveredObject, eventData, ExecuteEvents.dropHandler);

				eventData.pointerDrag = null;
				source.draggedObject = null;
			}

			var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hoveredObject);
			if (source.pressedObject == clickHandler && eventData.eligibleForClick)
				ExecuteEvents.Execute(clickHandler, eventData, ExecuteEvents.pointerClickHandler);

			eventData.rawPointerPress = null;
			eventData.pointerPress = null;
			eventData.eligibleForClick = false;
			source.pressedObject = null;
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

			List<RaycastResult> results = new List<RaycastResult>();
			eventSystem.RaycastAll(eventData, results);
			eventData.pointerCurrentRaycast = FindFirstRaycast(results);
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