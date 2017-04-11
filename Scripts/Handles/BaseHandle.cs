#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	class BaseHandle : MonoBehaviour, ISelectionFlags, IRayBeginDragHandler, IRayDragHandler, IRayEndDragHandler,
		IRayEnterHandler, IRayExitHandler, IRayHoverHandler, IPointerClickHandler, IDropReceiver, IDroppable
	{
		public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
		[SerializeField]
		[FlagsProperty]
		SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

		const int k_DefaultCapacity = 2; // i.e. 2 controllers

		protected readonly List<Transform> m_HoverSources = new List<Transform>(k_DefaultCapacity);
		protected readonly List<Transform> m_DragSources = new List<Transform>(k_DefaultCapacity);
		protected DateTime m_LastClickTime;

		public Vector3 startDragPosition { get; protected set; }

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
				{
					eventData.rayOrigin = m_HoverSources[i];
					OnHandleHoverEnded(eventData);
				}
				m_HoverSources.Clear();

				for (int i = 0; i < m_DragSources.Count; i++)
				{
					eventData.rayOrigin = m_DragSources[i];
					OnHandleDragEnded(eventData);
				}
				m_DragSources.Clear();
			}
		}

		protected virtual HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new HandleEventData(eventData.rayOrigin, UIUtils.IsDirectEvent(eventData));
		}

		public void OnBeginDrag(RayEventData eventData)
		{
			if (!UIUtils.IsValidEvent(eventData, selectionFlags))
				return;

			m_DragSources.Add(eventData.rayOrigin);
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;

			var handleEventData = GetHandleEventData(eventData);

			//Double-click logic
			var timeSinceLastClick = (float) (DateTime.Now - m_LastClickTime).TotalSeconds;
			m_LastClickTime = DateTime.Now;
			if (UIUtils.IsDoubleClick(timeSinceLastClick))
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
#endif
