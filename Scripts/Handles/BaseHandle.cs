using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public abstract class BaseHandle : MonoBehaviour, IRayBeginDragHandler, IRayEndDragHandler, IRayEnterHandler, IRayExitHandler
	{
		public delegate void DragEventCallback(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData));

		public event DragEventCallback onHandleBeginDrag;
		public event DragEventCallback onHandleDrag;
		public event DragEventCallback onHandleEndDrag;

		protected Transform m_RayOrigin;
		protected bool m_Hovering;
		protected bool m_Dragging;

		public Vector3 startDragPosition { get; protected set; }

		public virtual void OnBeginDrag(RayEventData eventData)
		{
			m_RayOrigin = eventData.rayOrigin;
			m_Dragging = true;
			startDragPosition = eventData.pointerCurrentRaycast.worldPosition;
		}

		public virtual void OnEndDrag(RayEventData eventData)
		{
			m_Dragging = false;
		}

		public virtual void OnRayEnter(RayEventData eventData)
		{
			if (!m_Dragging)
				m_RayOrigin = eventData.rayOrigin;

			m_Hovering = true;
		}

		public virtual void OnRayExit(RayEventData eventData)
		{
			m_Hovering = false;
		}

		protected virtual void OnHandleBeginDrag()
		{
			if (onHandleBeginDrag != null)
				onHandleBeginDrag(this);
		}

		protected virtual void OnHandleDrag(HandleDragEventData eventData)
		{
			if (onHandleDrag != null)
				onHandleDrag(this, eventData);
		}

		protected virtual void OnHandleEndDrag()
		{
			if (onHandleEndDrag != null)
				onHandleEndDrag(this);
		}
	}
}