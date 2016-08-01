using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class PlaneHandle : BaseHandle
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
		m_PlaneCollider.transform.rotation = transform.rotation;

		//m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		m_PlaneCollider.GetComponent<Renderer>().sharedMaterial = m_DebugMaterial;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		OnHandleBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;

		var delta = worldPosition - m_LastPosition;
		m_LastPosition = worldPosition;

		// Flip raycast blocking plane
		if (Vector3.Dot(m_PlaneCollider.transform.forward, m_RayOrigin.forward) < 0)
			m_PlaneCollider.transform.Rotate(m_PlaneCollider.transform.up, 180.0f);

		delta = transform.InverseTransformVector(delta);
		delta.z = 0;
		delta = transform.TransformVector(delta);

		OnHandleDrag(delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		OnHandleEndDrag();
	}

	void OnDestroy()
	{
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);
	}
}
