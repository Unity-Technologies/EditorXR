using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class RadialHandle : BaseHandle, IRayHoverHandler, IRayDragHandler
	{
		[SerializeField]
		private float m_TurnSpeed;
		[SerializeField]
		private Transform m_HandleTip;

		private Plane m_Plane;
		private Vector3 m_LastPosition;
		private Vector3 m_LastDragForward;
		private Vector3 m_DragTangent;

		private void OnDisable()
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

			Transform rayOrigin = eventData.rayOrigin;

			m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;
			m_LastDragForward = rayOrigin.forward;

			m_Plane.SetNormalAndPosition(rayOrigin.forward, transform.position);

			m_DragTangent = Vector3.Cross(transform.up, startDragPosition - transform.position);

			UpdateHandleTip(eventData);

			OnHandleBeginDrag();
		}

		public void OnDrag(RayEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;

			Vector3 worldPosition = m_LastPosition;

			float distance;
			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Abs(distance));

			m_DragTangent = Vector3.Cross(transform.up, (startDragPosition - transform.position).normalized);
			var angle = m_TurnSpeed * Vector3.Angle(rayOrigin.forward, m_LastDragForward) *
						Vector3.Dot((worldPosition - m_LastPosition).normalized, m_DragTangent);
			var delta = Quaternion.AngleAxis(angle, transform.up);

			m_LastPosition = worldPosition;
			m_LastDragForward = rayOrigin.forward;

			if (m_HandleTip != null)
				m_HandleTip.RotateAround(transform.position, transform.up, angle);

			OnHandleDrag(new HandleDragEventData(delta, rayOrigin));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);

			UpdateHandleTip(eventData);

			OnHandleEndDrag();
		}
	}
}