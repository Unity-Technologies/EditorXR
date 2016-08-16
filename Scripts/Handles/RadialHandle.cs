using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class RadialHandle : BaseHandle, IRayHoverHandler, IRayDragHandler
	{
		[SerializeField] private float m_TurnSpeed;
		[SerializeField] private Transform m_HandleTip;

		private Collider m_PlaneCollider;
		private Vector3 m_LastPosition;
		private Vector3 m_LastDragForward;
		private Collider m_Collider;
		private Vector3 m_DragTangent;

		protected void Awake()
		{
			m_Collider = GetComponent<Collider>();
		}

		void OnDisable()
		{
			if (m_HandleTip != null)
				m_HandleTip.gameObject.SetActive(false);
		}

		public void OnRayHover(RayEventData eventData)
		{
			UpdateHandleTip(eventData);
		}

		private void UpdateHandleTip(RayEventData eventData)
		{
			if (m_HandleTip != null)
			{
				m_HandleTip.gameObject.SetActive(m_Hovering || m_Dragging);

				if (m_Hovering && !m_Dragging) // Reposition handle tip based on current raycast position when hovering (dragging is handled in OnDrag)
				{
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

		public override void OnRayEnter(RayEventData eventData)
		{
			base.OnRayEnter(eventData);
			UpdateHandleTip(eventData);
		}

		public override void OnRayExit(RayEventData eventData)
		{
			base.OnRayExit(eventData);
			UpdateHandleTip(eventData);
		}

		public override void OnBeginDrag(RayEventData eventData)
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

			m_DragTangent = Vector3.Cross(transform.up, startDragPosition - transform.position);

			UpdateHandleTip(eventData);

			OnHandleBeginDrag();
		}

		public void OnDrag(RayEventData eventData)
		{
			// Flip raycast blocking plane
			if (Vector3.Dot(m_PlaneCollider.transform.forward, m_RayOrigin.forward) < 0f)
				m_PlaneCollider.transform.forward = -m_PlaneCollider.transform.forward;

			Vector3 worldPosition = m_LastPosition;
			RaycastHit hit;
			if (m_PlaneCollider.Raycast(new Ray(m_RayOrigin.position, m_RayOrigin.forward), out hit, Mathf.Infinity))
				worldPosition = hit.point;
			m_DragTangent = Vector3.Cross(transform.up, (startDragPosition - transform.position).normalized);
			var angle = m_TurnSpeed * Vector3.Angle(m_RayOrigin.forward, m_LastDragForward) *
						Vector3.Dot((worldPosition - m_LastPosition).normalized, m_DragTangent);
			var delta = Quaternion.AngleAxis(angle, transform.up);

			m_LastPosition = worldPosition;
			m_LastDragForward = m_RayOrigin.forward;

			if (m_HandleTip != null)
				m_HandleTip.RotateAround(transform.position, transform.up, angle);

			OnHandleDrag(new HandleDragEventData(delta));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);
			m_Collider.enabled = true;

			if (m_PlaneCollider != null)
				DestroyImmediate(m_PlaneCollider.gameObject);

			UpdateHandleTip(eventData);

			OnHandleEndDrag();
		}

		void OnDestroy()
		{
			if (m_PlaneCollider != null)
				DestroyImmediate(m_PlaneCollider.gameObject);
		}
	}
}