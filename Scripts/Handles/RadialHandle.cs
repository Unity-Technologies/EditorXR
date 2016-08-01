using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;

public class RadialHandle : BaseHandle
{
	private Collider m_PlaneCollider;
	private Vector3 m_LastPosition;
	private Vector3 m_LastDragForward;
	private Vector3 m_StartDragPosition;
	[SerializeField]
	private float m_TurnSpeed;

	private VRLineRenderer m_Tangent;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);

		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;
		m_LastDragForward = m_RayOrigin.forward;
		m_StartDragPosition = eventData.pointerCurrentRaycast.worldPosition;

		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		m_PlaneCollider = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Collider>();
		m_PlaneCollider.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_PlaneCollider.transform.localScale = Vector3.one*5000f;
		m_PlaneCollider.transform.position = transform.position;
		m_PlaneCollider.transform.forward = -transform.up;//m_RayOrigin.forward;
		
		m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		//m_PlaneCollider.GetComponent<Renderer>().sharedMaterial = m_DebugMaterial;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		var tangent = Vector3.Cross(transform.up, m_StartDragPosition - transform.position);

		OnHandleBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		//m_PlaneCollider.transform.forward = m_RayOrigin.forward;

		// Flip raycast blocking plane
		if (Vector3.Dot(m_PlaneCollider.transform.forward, m_RayOrigin.forward) < 0)
			m_PlaneCollider.transform.Rotate(m_PlaneCollider.transform.up, 180.0f);

		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;
		var tangent = Vector3.Cross(transform.up, (m_StartDragPosition - transform.position).normalized);
		var angle = m_TurnSpeed * Vector3.Angle(m_RayOrigin.forward, m_LastDragForward) * Vector3.Dot((worldPosition - m_LastPosition).normalized, tangent);//.magnitude * ()
		Debug.DrawRay(m_StartDragPosition, tangent*Vector3.Dot((worldPosition - m_LastPosition).normalized, tangent));
		var delta = Quaternion.AngleAxis(angle, transform.up);


		m_LastPosition = worldPosition;
		m_LastDragForward = m_RayOrigin.forward;
		OnHandleDrag(Vector3.zero, delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);

		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		DestroyImmediate(m_Tangent);

		OnHandleEndDrag();
	}

	void OnDestroy()
	{
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);
	}


}
