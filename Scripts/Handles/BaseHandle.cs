using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class BaseHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField]
	protected Material m_DebugMaterial;

	public delegate void DragEventCallback(BaseHandle handle, Vector3 deltaPosition = default(Vector3), Quaternion deltaRotation = default(Quaternion));

	public event DragEventCallback onHandleBeginDrag;
	public event DragEventCallback onHandleDrag;
	public event DragEventCallback onHandleEndDrag;

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
