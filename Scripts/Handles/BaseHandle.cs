using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;
using System;

public class BaseHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{

	public delegate void DragEventCallback(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion));

	public event DragEventCallback onHandleBeginDrag;
	public event DragEventCallback onHandleDrag;
	public event DragEventCallback onHandleEndDrag;

	protected Transform m_RayOrigin;
	protected Renderer m_Renderer;
	protected bool m_Hovering = false;
	protected bool m_Dragging = false;

	protected virtual void Awake()
	{
		m_Renderer = GetComponent<Renderer>();
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
		m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
		m_Dragging = true;
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
		if(!m_Dragging)
			m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
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

	protected virtual void OnHandleDrag(Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion))
	{
		if (onHandleDrag != null)
			onHandleDrag(this, deltaPosition, deltaRotation);
	}

	protected virtual void OnHandleEndDrag()
	{
		if (onHandleEndDrag != null)
			onHandleEndDrag(this);
	}
}
