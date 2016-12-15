using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.UI;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public class BaseHandle : MonoBehaviour, ISelectionFlags, IRayBeginDragHandler, IRayDragHandler, IRayEndDragHandler, IRayEnterHandler, IRayExitHandler, IRayHoverHandler, IDropReceiver, IDroppable
	{
		public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
		[SerializeField]
		[FlagsProperty]
		private SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

		private const int kDefaultCapacity = 2; // i.e. 2 controllers

		protected readonly List<Transform> m_HoverSources = new List<Transform>(kDefaultCapacity);
		protected readonly List<Transform> m_DragSources = new List<Transform>(kDefaultCapacity);
		protected DateTime m_LastClickTime;

		public Vector3 startDragPosition { get; protected set; }

		public Func<BaseHandle, object, bool> canDrop;
		public Action<BaseHandle, object> receiveDrop;
		public Func<BaseHandle, object> getDropObject;
		public event Action<BaseHandle> dropHoverStarted = delegate {};
		public event Action<BaseHandle> dropHoverEnded = delegate {};

		public event Action<BaseHandle, HandleEventData> dragStarted = delegate { };
		public event Action<BaseHandle, HandleEventData> dragging = delegate { };
		public event Action<BaseHandle, HandleEventData> dragEnded = delegate { };

		public event Action<BaseHandle, HandleEventData> doubleClick = delegate { };

		public event Action<BaseHandle, HandleEventData> hoverStarted = delegate { };
		public event Action<BaseHandle, HandleEventData> hovering = delegate { };
		public event Action<BaseHandle, HandleEventData> hoverEnded = delegate { };

		void Awake()
		{
			// Put this object in the UI layer so that it is hit by UI raycasts
			gameObject.layer = LayerMask.NameToLayer("UI");
		}

		private void OnDisable()
		{
			if (m_HoverSources.Count > 0 || m_DragSources.Count > 0)
			{
				var eventData = GetHandleEventData(new RayEventData(EventSystem.current));
				for (int i = 0; i < m_HoverSources.Count; i++)
					OnHandleHoverEnded(eventData);
				m_HoverSources.Clear();

				for (int i = 0; i < m_DragSources.Count; i++)
					OnHandleDragEnded(eventData);
				m_DragSources.Clear();
			}
		}

		protected virtual HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new HandleEventData(eventData.rayOrigin, U.UI.IsDirectEvent(eventData));
		}

		public void OnBeginDrag(RayEventData eventData)
		{
			if (!U.UI.IsValidEvent(eventData, selectionFlags))
				return;

			m_DragSources.Add(eventData.rayOrigin);
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;

			var handleEventData = GetHandleEventData(eventData);

			//Double-click logic
			var timeSinceLastClick = (float) (DateTime.Now - m_LastClickTime).TotalSeconds;
			m_LastClickTime = DateTime.Now;
			if (U.UI.IsDoubleClick(timeSinceLastClick))
			{
				OnDoubleClick(handleEventData);
			}

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
			if (!U.UI.IsValidEvent(eventData, selectionFlags))
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
				if (!handleEventData.direct && m_HoverSources.Remove(eventData.rayOrigin))
				{
					OnHandleHoverEnded(handleEventData);
					return;
				}

				if (handleEventData.direct && !m_HoverSources.Contains(eventData.rayOrigin))
				{
					m_HoverSources.Add(eventData.rayOrigin);
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
			hoverStarted(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleHovering(HandleEventData eventData)
		{
			hovering(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleHoverEnded(HandleEventData eventData)
		{
			hoverEnded(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleDragStarted(HandleEventData eventData)
		{
			dragStarted(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleDragging(HandleEventData eventData)
		{
			dragging(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleDragEnded(HandleEventData eventData)
		{
			dragEnded(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnDoubleClick(HandleEventData eventData)
		{
			doubleClick(this, eventData);
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
			if (getDropObject != null)
				return getDropObject(this);

			return null;
		}

		public void OnDropHoverStarted()
		{
			dropHoverStarted(this);
		}

		public void OnDropHoverEnded()
		{
			dropHoverEnded(this);
		}
	}
}