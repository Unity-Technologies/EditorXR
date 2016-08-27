using System;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public class BaseHandle : MonoBehaviour, IRayBeginDragHandler, IRayDragHandler, IRayEndDragHandler, IRayEnterHandler, IRayExitHandler, IRayHoverHandler
	{
		public event Action<BaseHandle, HandleDragEventData> onHandleBeginDrag;
		public event Action<BaseHandle, HandleDragEventData> onHandleDrag;
		public event Action<BaseHandle, HandleDragEventData> onHandleEndDrag;

		public event Action<BaseHandle, HandleDragEventData> onDoubleClick;

		public event Action<BaseHandle, HandleDragEventData> onHoverEnter;
		public event Action<BaseHandle, HandleDragEventData> onHover;
		public event Action<BaseHandle, HandleDragEventData> onHoverExit;

		protected bool m_Hovering;
		protected bool m_Dragging;

		protected DateTime m_LastClickTime;
		//HACK: need a better way to get this into HandleDragEventData
		protected bool m_IsDirect;

		public Vector3 startDragPosition { get; protected set; }

		public virtual void OnBeginDrag(RayEventData eventData)
		{
			m_Dragging = true;
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;
			m_IsDirect = eventData.pointerPressRaycast.isValid && eventData.pointerCurrentRaycast.distance < eventData.pointerLength;

			//Double-click logic
			var timeSinceLastClick = (float)(DateTime.Now - m_LastClickTime).TotalSeconds;
			m_LastClickTime = DateTime.Now;
			if (U.Input.DoubleClick(timeSinceLastClick))
			{
				OnDoubleClick(new HandleDragEventData(eventData.rayOrigin));
			}
		}

		public virtual void OnDrag(RayEventData eventData)
		{
		}

		public virtual void OnEndDrag(RayEventData eventData)
		{
			m_IsDirect = eventData.pointerPressRaycast.isValid && eventData.pointerCurrentRaycast.distance < eventData.pointerLength;
			m_Dragging = false;
		}

		public virtual void OnRayEnter(RayEventData eventData)
		{
			m_Hovering = true;
			if (onHoverEnter != null)
				onHoverEnter(this, new HandleDragEventData(eventData.rayOrigin) { direct = eventData.pointerCurrentRaycast.distance < eventData.pointerLength });
		}

		public virtual void OnRayHover(RayEventData eventData) {
			if (eventData.pointerCurrentRaycast.distance > eventData.pointerLength)
			{
				if (m_Hovering)
				{
					m_Hovering = false;
					OnRayExit(eventData);
				}
				return;
			}
			if (!m_Hovering)
			{
				m_Hovering = true;
				OnRayEnter(eventData);
			}
			if (onHover != null)
				onHover(this, new HandleDragEventData(eventData.rayOrigin) { direct = eventData.pointerCurrentRaycast.distance < eventData.pointerLength });
		}

		public virtual void OnRayExit(RayEventData eventData)
		{
			m_Hovering = false;
			if (onHoverExit != null)
				onHoverExit(this, new HandleDragEventData(eventData.rayOrigin) { direct = eventData.pointerCurrentRaycast.distance < eventData.pointerLength });
		}

		protected virtual void OnHandleBeginDrag(HandleDragEventData eventData = default(HandleDragEventData))
		{
			if (onHandleBeginDrag != null)
			{
				eventData.direct = m_IsDirect;
				onHandleBeginDrag(this, eventData);
			}
		}

		protected virtual void OnHandleDrag(HandleDragEventData eventData)
		{
			if (onHandleDrag != null)
			{
				eventData.direct = m_IsDirect;
				onHandleDrag(this, eventData);
			}
		}

		protected virtual void OnHandleEndDrag(HandleDragEventData eventData = default(HandleDragEventData))
		{
			if (onHandleEndDrag != null)
			{
				eventData.direct = m_IsDirect;
				onHandleEndDrag(this, eventData);
			}
		}

		protected virtual void OnDoubleClick(HandleDragEventData eventData = default(HandleDragEventData))
		{
			if (onDoubleClick != null)
			{
				eventData.direct = m_IsDirect;
				onDoubleClick(this, eventData);
			}
		}
	}
}