using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public class BaseHandle : MonoBehaviour, IRayBeginDragHandler, IRayDragHandler, IRayEndDragHandler, IRayEnterHandler, IRayExitHandler, IRayHoverHandler
	{
		[Flags]
		public enum HandleFlags
		{
			Ray = 1 << 0,
			Direct = 1 << 1
		}

		public event Action<BaseHandle, HandleEventData> handleDragging = delegate { };
		public event Action<BaseHandle, HandleEventData> handleDrag = delegate { };
		public event Action<BaseHandle, HandleEventData> handleDragged = delegate { };

		public event Action<BaseHandle, HandleEventData> doubleClick = delegate { };

		public event Action<BaseHandle, HandleEventData> hovering = delegate { };
		public event Action<BaseHandle, HandleEventData> hover = delegate { };
		public event Action<BaseHandle, HandleEventData> hovered = delegate { };

		public HandleFlags handleFlags { get { return m_HandleFlags; } set { m_HandleFlags = value; } }
		[SerializeField]
		[FlagsProperty]
		private HandleFlags m_HandleFlags = HandleFlags.Ray | HandleFlags.Direct;

		private const int kDefaultCapacity = 2; // i.e. 2 controllers

		protected readonly List<Transform> m_HoverSources = new List<Transform>(kDefaultCapacity);
		protected readonly List<Transform> m_DragSources = new List<Transform>(kDefaultCapacity);
		protected DateTime m_LastClickTime;

		public Vector3 startDragPosition { get; protected set; }

		private void OnDisable()
		{
			if (m_HoverSources.Count > 0 || m_DragSources.Count > 0)
			{
				var eventData = GetHandleEventData(new RayEventData(EventSystem.current));
				for (int i = 0; i < m_HoverSources.Count; i++)
					OnHandleRayExit(eventData);
				m_HoverSources.Clear();

				for (int i = 0; i < m_DragSources.Count; i++)
					OnHandleEndDrag(eventData);
				m_DragSources.Clear();
			}
		}

		protected virtual HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new HandleEventData(eventData.rayOrigin, IsDirectSelection(eventData));
		}

		protected virtual bool IsDirectSelection(RayEventData eventData)
		{
			return eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.distance <= eventData.pointerLength;
		}

		protected virtual bool ValidEvent(HandleEventData eventData)
		{
			if ((handleFlags & HandleFlags.Direct) != 0 && eventData.direct)
				return true;

			if ((handleFlags & HandleFlags.Ray) != 0)
				return true;

			return false;
		}

		public void OnBeginDrag(RayEventData eventData)
		{
			var handleEventData = GetHandleEventData(eventData);
			if (!ValidEvent(handleEventData))
				return;

			m_DragSources.Add(eventData.rayOrigin);
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;

			//Double-click logic
			var timeSinceLastClick = (float) (DateTime.Now - m_LastClickTime).TotalSeconds;
			m_LastClickTime = DateTime.Now;
			if (U.Input.DoubleClick(timeSinceLastClick))
			{
				OnDoubleClick(handleEventData);
			}

			OnHandleBeginDrag(handleEventData);
		}

		public void OnDrag(RayEventData eventData)
		{
			if (m_DragSources.Count > 0)
				OnHandleDrag(GetHandleEventData(eventData));
		}

		public void OnEndDrag(RayEventData eventData)
		{
			if (m_DragSources.Remove(eventData.rayOrigin))
				OnHandleEndDrag(GetHandleEventData(eventData));
		}

		public void OnRayEnter(RayEventData eventData)
		{
			var handleEventData = GetHandleEventData(eventData);
			if (!ValidEvent(handleEventData))
				return;

			m_HoverSources.Add(eventData.rayOrigin);
			OnHandleRayEnter(handleEventData);
		}

		public void OnRayHover(RayEventData eventData)
		{
			var handleEventData = GetHandleEventData(eventData);

			// Direct selection has special handling for enter/exit since those events may not have been called
			// because the pointer wasn't close enough to the handle
			if (handleFlags == HandleFlags.Direct)
			{
				if (!handleEventData.direct && m_HoverSources.Remove(eventData.rayOrigin))
				{
					OnHandleRayExit(handleEventData);
					return;
				}

				if (handleEventData.direct && !m_HoverSources.Contains(eventData.rayOrigin))
				{
					m_HoverSources.Add(eventData.rayOrigin);
					OnHandleRayEnter(handleEventData);
				}
			}

			if (m_HoverSources.Count > 0)
				OnHandleRayHover(GetHandleEventData(eventData));
		}

		public void OnRayExit(RayEventData eventData)
		{
			if (m_HoverSources.Remove(eventData.rayOrigin))
				OnHandleRayExit(GetHandleEventData(eventData));
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleRayEnter(HandleEventData eventData)
		{
			hovering(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleRayHover(HandleEventData eventData)
		{
			hover(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleRayExit(HandleEventData eventData)
		{
			hovered(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleBeginDrag(HandleEventData eventData)
		{
			handleDragging(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleDrag(HandleEventData eventData)
		{
			handleDrag(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnHandleEndDrag(HandleEventData eventData)
		{
			handleDragged(this, eventData);
		}

		/// <summary>
		/// Override to modify event data prior to raising event (requires calling base method at the end)
		/// </summary>
		protected virtual void OnDoubleClick(HandleEventData eventData)
		{
			doubleClick(this, eventData);
		}
	}
}