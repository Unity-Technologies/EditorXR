using UnityEngine;
using UnityEngine.EventSystems;

public class PlaneHandle : BaseHandle
{
	[SerializeField]
	private Material m_PlaneMaterial;
	private const float kPlaneScale = 1000f;
	private Collider m_PlaneCollider;
	private Collider m_Collider;
	private Vector3 m_LastPosition;

	protected override void Awake()
	{
		base.Awake();
		m_Collider = GetComponent<Collider>();
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);

		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		m_PlaneCollider = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Collider>();
		m_PlaneCollider.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_PlaneCollider.transform.localScale = Vector3.one * kPlaneScale;
		m_PlaneCollider.transform.position = transform.position;
		m_PlaneCollider.transform.rotation = transform.rotation;

		m_PlaneCollider.GetComponent<Renderer>().sharedMaterial = m_PlaneMaterial;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		OnHandleBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		// Flip raycast blocking plane
		if (Vector3.Dot(m_PlaneCollider.transform.forward, m_RayOrigin.forward) < 0f)
			m_PlaneCollider.transform.forward = -m_PlaneCollider.transform.forward;

		var worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;

		var delta = worldPosition - m_LastPosition;
		m_LastPosition = worldPosition;

		delta = transform.InverseTransformVector(delta);
		delta.z = 0;
		delta = transform.TransformVector(delta);

		OnHandleDrag(delta);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		base.OnEndDrag(eventData);
		m_Collider.enabled = true;

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
