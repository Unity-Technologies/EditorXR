using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputNew;
using UnityEngine.VR.Proxies;

namespace UnityEngine.VR.Modules
{
	public class MultipleRayInputModule : PointerInputModule
	{
		[SerializeField]
		public Camera EventCameraPrefab; // Camera to be instantiated and assigned to EventCamera property

		private readonly List<RaycastSource> m_RaycastSources = new List<RaycastSource>();
		private Dictionary<Transform, int> m_RayOriginToPointerID = new Dictionary<Transform, int>();
		private List<RayEventData> PointEvents = new List<RayEventData>();
		private List<GameObject> CurrentPoint = new List<GameObject>();
		private List<GameObject> CurrentPressed = new List<GameObject>();
		private List<GameObject> CurrentDragging = new List<GameObject>();

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
			public UIActions actionMapInput;

			public RaycastSource(IProxy proxy, Transform rayOrigin, UIActions actionMapInput)
			{
				this.proxy = proxy;
				this.rayOrigin = rayOrigin;
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
				m_RayOriginToPointerID.Add(rayOrigin, m_RaycastSources.Count);
				m_RaycastSources.Add(new RaycastSource(proxy, rayOrigin, actions));
			}
			else
				Debug.LogError("Failed to get ray origin transform for node " + node + " from proxy " + proxy);
		}

		private Transform GetRayOrigin(int index)
		{
			return m_RaycastSources[index].rayOrigin;
		}

		public PointerEventData GetPointerEventData(Transform rayOrigin)
		{
			int id;
			if (m_RayOriginToPointerID.TryGetValue(rayOrigin, out id))
			{
				if (id >= 0 && id < PointEvents.Count)
					return PointEvents[id];
			}

			return null;
		}

		public override void Process()
		{
			ExecuteUpdateOnSelectedObject();

			if (m_EventCamera == null)
				return;

			//Process events for all different transforms in RayOrigins
			for (int i = 0; i < m_RaycastSources.Count; i++)
			{
				// Expand lists if needed
				while (i >= CurrentPoint.Count)
					CurrentPoint.Add(null);
				while (i >= CurrentPressed.Count)
					CurrentPressed.Add(null);
				while (i >= CurrentDragging.Count)
					CurrentDragging.Add(null);
				while (i >= PointEvents.Count)
					PointEvents.Add(new RayEventData(base.eventSystem));

				PointEvents[i].pointerId = i;

				if (!m_RaycastSources[i].proxy.active)
					continue;

				CurrentPoint[i] = GetRayIntersection(i); // Check all currently running raycasters

				var rayEventData = PointEvents[i] as RayEventData;
				rayEventData.rayOrigin = GetRayOrigin(i);

				HandlePointerExitAndEnter(PointEvents[i], CurrentPoint[i]); // Send enter and exit events

				// Activate actionmap input only if pointer is interacting with something
				m_RaycastSources[i].actionMapInput.active = (CurrentPoint[i] != null && CurrentPoint[i].layer == UILayer) || 
															CurrentPressed[i] != null ||
															CurrentDragging[i] != null;

				if (!m_RaycastSources[i].actionMapInput.active)
					continue;

				// Send select pressed and released events
				if (m_RaycastSources[i].actionMapInput.select.wasJustPressed)
					OnSelectPressed(i);

				if (m_RaycastSources[i].actionMapInput.select.wasJustReleased)
					OnSelectReleased(i);

				if (CurrentDragging[i] != null)
				{
					ExecuteEvents.Execute(CurrentDragging[i], PointEvents[i], ExecuteEvents.dragHandler);
					ExecuteEvents.Execute(CurrentDragging[i], PointEvents[i], ExecuteRayEvents.dragHandler);
				}

				// Send scroll events
				if (CurrentPressed[i] != null)
				{
					PointEvents[i].scrollDelta = new Vector2(0f, m_RaycastSources[i].actionMapInput.verticalScroll.value);
					ExecuteEvents.ExecuteHierarchy(CurrentPoint[i], PointEvents[i], ExecuteEvents.scrollHandler);
				}

				m_PointerData[i] = PointEvents[i];
			}
		}

		private RayEventData CloneEventData(RayEventData eventData)
		{			
			RayEventData clone = new RayEventData(base.eventSystem);
			clone.rayOrigin = eventData.rayOrigin;
			clone.hovered = new List<GameObject>(eventData.hovered);
			clone.pointerEnter = eventData.pointerEnter;
			clone.pointerCurrentRaycast = eventData.pointerCurrentRaycast;

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

		private void OnSelectPressed(int i)
		{
			Deselect();

			PointEvents[i].pressPosition = PointEvents[i].position;
			PointEvents[i].pointerPressRaycast = PointEvents[i].pointerCurrentRaycast;
			PointEvents[i].pointerPress = CurrentPoint[i];

			if (CurrentPoint[i] != null) // Pressed when pointer is over something
			{
				CurrentPressed[i] = CurrentPoint[i];
				GameObject newPressed = ExecuteEvents.ExecuteHierarchy(CurrentPressed[i], PointEvents[i], ExecuteEvents.pointerDownHandler);

				if (newPressed == null) // Gameobject does not have pointerDownHandler in hierarchy, but may still have click handler
					newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentPressed[i]);

				if (newPressed != null)
				{
					CurrentPressed[i] = newPressed; // Set current pressed to gameObject that handles the pointerDown event, not the root object
					PointEvents[i].pointerPress = newPressed;
					Select(CurrentPressed[i]);
					PointEvents[i].eligibleForClick = true;
				}
				ExecuteEvents.Execute(CurrentPressed[i], PointEvents[i], ExecuteEvents.beginDragHandler);
				ExecuteEvents.Execute(CurrentPressed[i], PointEvents[i], ExecuteRayEvents.beginDragHandler);
				PointEvents[i].pointerDrag = CurrentPressed[i];
				CurrentDragging[i] = CurrentPressed[i];
			}
		}

		private void OnSelectReleased(int i)
		{
			if (CurrentPressed[i])
				ExecuteEvents.Execute(CurrentPressed[i], PointEvents[i], ExecuteEvents.pointerUpHandler);

			if (CurrentDragging[i])
			{
				ExecuteEvents.Execute(CurrentDragging[i], PointEvents[i], ExecuteEvents.endDragHandler);
				ExecuteEvents.Execute(CurrentDragging[i], PointEvents[i], ExecuteRayEvents.endDragHandler);

				if (CurrentPoint[i] != null)
					ExecuteEvents.ExecuteHierarchy(CurrentPoint[i], PointEvents[i], ExecuteEvents.dropHandler);

				PointEvents[i].pointerDrag = null;
				CurrentDragging[i] = null;
			}

			var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(CurrentPoint[i]);
			if (CurrentPressed[i] == clickHandler && PointEvents[i].eligibleForClick)
				ExecuteEvents.Execute(clickHandler, PointEvents[i], ExecuteEvents.pointerClickHandler);

			PointEvents[i].rawPointerPress = null;
			PointEvents[i].pointerPress = null;
			PointEvents[i].eligibleForClick = false;
			CurrentPressed[i] = null;
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

		private GameObject GetRayIntersection(int i)
		{
			GameObject hit = null;
			// Move camera to position and rotation for the ray origin
			m_EventCamera.transform.position = m_RaycastSources[i].rayOrigin.position;
			m_EventCamera.transform.rotation = m_RaycastSources[i].rayOrigin.rotation;

			PointEvents[i].Reset();
			PointEvents[i].delta = Vector2.zero;
			PointEvents[i].position = m_EventCamera.pixelRect.center;
			PointEvents[i].scrollDelta = Vector2.zero;

			List<RaycastResult> results = new List<RaycastResult>();
			eventSystem.RaycastAll(PointEvents[i], results);
			PointEvents[i].pointerCurrentRaycast = FindFirstRaycast(results);
			hit = PointEvents[i].pointerCurrentRaycast.gameObject;

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