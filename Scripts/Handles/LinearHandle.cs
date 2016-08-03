using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class LinearHandle : BaseHandle
{
	private const float kMaxDragDistance = 1000f;
	private Collider m_PlaneCollider;
	private Vector3 m_LastPosition;

	[SerializeField]
	private Transform m_HandleTip;

	void Update()
	{
		if (m_HandleTip != null)
		{
			if (m_Hovering || m_Dragging) // Reposition handle tip based on current raycast position when hovering or dragging
			{
				m_HandleTip.gameObject.SetActive(true);
				var eventData = ((MultipleRayInputModule)EventSystem.current.currentInputModule).GetPointerEventData(m_RayOrigin);
				if (eventData != null)
					m_HandleTip.position =
						transform.TransformPoint(new Vector3(0, 0,
							transform.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition).z));
			}
			else
				m_HandleTip.gameObject.SetActive(false);
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
		if (m_PlaneCollider != null)
			DestroyImmediate(m_PlaneCollider.gameObject);

		m_PlaneCollider = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<Collider>();
		m_PlaneCollider.transform.SetParent(eventData.pressEventCamera.transform.parent);
		m_PlaneCollider.transform.localScale = Vector3.one * kMaxDragDistance;
		m_PlaneCollider.transform.position = transform.position;

		var forward = transform.InverseTransformVector(m_RayOrigin.forward);
		forward.z = 0;
		m_PlaneCollider.transform.forward = transform.TransformVector(forward);

		m_PlaneCollider.GetComponent<Renderer>().enabled = false;
		m_PlaneCollider.gameObject.layer = LayerMask.NameToLayer("UI");

		OnHandleBeginDrag();
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
