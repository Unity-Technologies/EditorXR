using System;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public abstract class BaseHandle : MonoBehaviour, IRayBeginDragHandler, IRayEndDragHandler, IRayEnterHandler, IRayExitHandler
	{
		//Q: Why not an Action?
		public delegate void DragEventCallback(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData));

		public event DragEventCallback onHandleBeginDrag;
		public event DragEventCallback onHandleDrag;
		public event DragEventCallback onHandleEndDrag;

		public event Action<BaseHandle> onHoverEnter;
		public event Action<BaseHandle> onHoverExit;

		protected bool m_Hovering;
		protected bool m_Dragging;

		public Vector3 startDragPosition { get; protected set; }

		public virtual void OnBeginDrag(RayEventData eventData)
		{
			m_Dragging = true;
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;
		}

		public virtual void OnEndDrag(RayEventData eventData)
		{
			m_Dragging = false;
		}

		public virtual void OnRayEnter(RayEventData eventData)
		{
			m_Hovering = true;
			if (onHoverEnter != null)
				onHoverEnter(this);
		}

		public virtual void OnRayExit(RayEventData eventData)
		{
			m_Hovering = false;
			if (onHoverExit != null)
				onHoverExit(this);
		}

		protected virtual void OnHandleBeginDrag(HandleDragEventData eventData = default(HandleDragEventData))
		{
			if (onHandleBeginDrag != null)
				onHandleBeginDrag(this, eventData);
		}

		protected virtual void OnHandleDrag(HandleDragEventData eventData)
		{
			if (onHandleDrag != null)
				onHandleDrag(this, eventData);
		}

		protected virtual void OnHandleEndDrag(HandleDragEventData eventData = default(HandleDragEventData))
		{
			if (onHandleEndDrag != null)
				onHandleEndDrag(this, eventData);
		}
	}
}