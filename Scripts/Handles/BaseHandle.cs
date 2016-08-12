using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	/// <summary>
	/// Base class for providing draggable handles in 3D (requires PhysicsRaycaster)
	/// </summary>
	public abstract class BaseHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public delegate void DragEventCallback(BaseHandle handle, HandleDragEventData eventData = default(HandleDragEventData));

		public event DragEventCallback onHandleBeginDrag;
		public event DragEventCallback onHandleDrag;
		public event DragEventCallback onHandleEndDrag;

		protected Transform m_RayOrigin;
		protected Renderer m_Renderer;
		protected bool m_Hovering;
		protected bool m_Dragging;
		protected Node m_Node;
		protected Vector3 m_StartDragPosition;

		public Vector3 startDragPosition
		{
			get { return m_StartDragPosition; }
		}

		protected virtual void Awake()
		{
			m_Renderer = GetComponent<Renderer>();
		}

		public virtual void OnBeginDrag(PointerEventData eventData)
		{
			// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
			m_RayOrigin = ((MultipleRayInputModule) EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
			m_Dragging = true;
			m_StartDragPosition = eventData.pointerCurrentRaycast.worldPosition;
		}

		public virtual void OnDrag(PointerEventData eventData)
		{
		}

		public virtual void OnEndDrag(PointerEventData eventData)
		{
			m_Dragging = false;
		}

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
			if (!m_Dragging)
				m_RayOrigin = ((MultipleRayInputModule) EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
			m_Hovering = true;
		}

		public virtual void OnPointerExit(PointerEventData eventData)
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