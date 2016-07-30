using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public abstract class BaseDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField]
	protected Material m_DebugMaterial;

	public delegate void DragEventCallback(Vector3 delta);

	public event DragEventCallback onBeginDrag;
	public event DragEventCallback onDrag;
	public event DragEventCallback onEndDrag;

	protected Transform m_RayOrigin;
	protected Collider m_Collider;

	void Awake()
	{
		m_Collider = GetComponentInChildren<Collider>();
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		// Get ray origin transform from InputModule and pointerID because the event camera moves between multiple transforms
		m_RayOrigin = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetRayOrigin(eventData.pointerId);
		m_Collider.enabled = false;
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		m_Collider.enabled = true;
	}

	protected virtual void RaiseBeginDrag()
	{
		if (onBeginDrag != null)
			onBeginDrag(Vector3.zero);
	}

	protected virtual void RaiseDrag(Vector3 delta)
	{
		if (onDrag != null)
			onDrag(delta);
	}

	protected virtual void RaiseEndDrag()
	{
		if (onEndDrag != null)
			onEndDrag(Vector3.zero);
	}
}
