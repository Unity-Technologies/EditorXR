using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class RadialHandle : BaseHandle
{
	[SerializeField]
	private float m_TurnSpeed;
	[SerializeField]
	private Transform m_HandleTip;

	private Collider m_PlaneCollider;
	private Vector3 m_LastPosition;
	private Vector3 m_LastDragForward;
	private Collider m_Collider;
	private Vector3 m_DragTangent;

	protected override void Awake()
	{
		base.Awake();
		m_Collider = GetComponent<Collider>();
	}

	void Update()
	{
		if (m_HandleTip != null)
		{
			m_HandleTip.gameObject.SetActive(m_Hovering || m_Dragging);

			if (m_Hovering && !m_Dragging) // Reposition handle tip based on current raycast position when hovering or dragging
			{
				var eventData = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetPointerEventData(m_RayOrigin); // Get the current hover position from InputModule using ray origin
				if (eventData != null)
				{
					var newLocalPos = transform.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition);
					newLocalPos.y = 0;
					m_HandleTip.position = transform.TransformPoint(newLocalPos.normalized * 0.5f * transform.localScale.x);
					m_DragTangent = Vector3.Cross(transform.up, (m_HandleTip.position - transform.position).normalized);
					m_HandleTip.forward = m_DragTangent;
				}
			}
		}
	}

	void OnDisable()
	{
		if (m_HandleTip != null)
			m_HandleTip.gameObject.SetActive(false);
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);

		m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;
		m_LastDragForward = m_RayOrigin.forward;

		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		m_PlaneCollider = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Collider>();
		m_PlaneCollider.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_PlaneCollider.transform.localScale = Vector3.one*5000f;
		m_PlaneCollider.transform.position = transform.position;
		m_PlaneCollider.transform.forward = m_RayOrigin.forward;
		
		m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		m_Collider.enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		m_DragTangent = Vector3.Cross(transform.up, m_StartDragPosition - transform.position);

		OnHandleBeginDrag();
	}

	public override void OnDrag(PointerEventData eventData)
	{
		// Flip raycast blocking plane
		if (Vector3.Dot(m_PlaneCollider.transform.forward, m_RayOrigin.forward) < 0f)
			m_PlaneCollider.transform.forward = -m_PlaneCollider.transform.forward;

		Vector3 worldPosition = m_LastPosition;
		RaycastHit hit;
		if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
			worldPosition = hit.point;
		m_DragTangent = Vector3.Cross(transform.up, (m_StartDragPosition - transform.position).normalized);
		var angle = m_TurnSpeed * Vector3.Angle(m_RayOrigin.forward, m_LastDragForward) * Vector3.Dot((worldPosition - m_LastPosition).normalized, m_DragTangent);//.magnitude * ()
		var delta = Quaternion.AngleAxis(angle, transform.up);

		m_LastPosition = worldPosition;
		m_LastDragForward = m_RayOrigin.forward;

		if (m_HandleTip != null)
			m_HandleTip.RotateAround(transform.position, transform.up, angle);

		OnHandleDrag(Vector3.zero, delta);
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
