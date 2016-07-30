using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

public class RadialDraggable : BaseDraggable
{
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
		m_PlaneCollider.transform.localScale = Vector3.one*1000f;
		m_PlaneCollider.transform.position = transform.position;
		m_PlaneCollider.transform.forward = m_RayOrigin.forward;

		//m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		m_PlaneCollider.GetComponent<Renderer>().sharedMaterial = m_DebugMaterial;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		RaiseBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		m_PlaneCollider.transform.forward = m_RayOrigin.forward;

		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;

		var delta = new Vector3(0, Vector3.Angle(transform.InverseTransformPoint(m_LastPosition),
			transform.InverseTransformPoint(worldPosition)), 0);

		m_LastPosition = worldPosition;

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
