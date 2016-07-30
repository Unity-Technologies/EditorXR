using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class LinearDraggable : BaseDraggable
{
	private const float kMaxDragDistance = 100f;
	private Collider m_PlaneCollider;
	private Vector3 m_LastPosition;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);

		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		m_PlaneCollider = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Collider>();
		m_PlaneCollider.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_PlaneCollider.transform.localScale = Vector3.one * kMaxDragDistance;
		m_PlaneCollider.transform.position = transform.position;

		var forward = transform.InverseTransformVector(m_RayOrigin.forward);
		forward.z = 0;
		m_PlaneCollider.transform.forward = transform.TransformVector(forward);

		//m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		m_PlaneCollider.GetComponent<Renderer>().sharedMaterial = m_DebugMaterial;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		RaiseBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity)) //TODO cache collider
			worldPosition = hit.point;

		var delta = worldPosition - m_LastPosition;
		m_LastPosition = worldPosition;

		var forward = transform.InverseTransformVector(m_RayOrigin.forward);
		forward.z = 0;
		m_PlaneCollider.transform.forward = transform.TransformVector(forward);
				
		delta = transform.InverseTransformVector(delta);
		delta.x = 0;
		delta.y = 0;
		delta = transform.TransformVector(delta);

		RaiseDrag(delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		RaiseEndDrag();
	}

	void OnDestroy()
	{
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);
	}
}
