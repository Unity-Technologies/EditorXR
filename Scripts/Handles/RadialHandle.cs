using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class RadialHandle : BaseHandle
	{
		private class RadialHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public RadialHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

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

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new RadialHandleEventData(eventData.rayOrigin, IsDirectSelection(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
		}

		protected override void OnHandleRayHover(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as RadialHandleEventData);
		}

		private void UpdateHandleTip(RadialHandleEventData eventData)
		{
			if (m_HandleTip != null)
			{
				m_HandleTip.gameObject.SetActive(m_HoverSources.Count > 0 || m_DragSources.Count > 0);

				if (m_HoverSources.Count > 0 || m_DragSources.Count > 0) // Reposition handle tip based on current raycast position when hovering (dragging is handled in OnDrag)
				{
					if (eventData != null)
					{
						var newLocalPos = transform.InverseTransformPoint(eventData.raycastHitWorldPosition);
						newLocalPos.y = 0;
						m_HandleTip.position = transform.TransformPoint(newLocalPos.normalized * 0.5f * transform.localScale.x);
						m_DragTangent = Vector3.Cross(transform.up, (m_HandleTip.position - transform.position).normalized);
						m_HandleTip.forward = m_DragTangent;
					}
				}
			}
		}

		protected override void OnHandleRayEnter(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as RadialHandleEventData);
			base.OnHandleRayEnter(eventData);
		}

		protected override void OnHandleRayExit(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as RadialHandleEventData);
			base.OnHandleRayExit(eventData);
		}

		protected override void OnHandleBeginDrag(HandleEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;

			var radialEventData = eventData as RadialHandleEventData;
			m_LastPosition = radialEventData.raycastHitWorldPosition;
			m_LastDragForward = rayOrigin.forward;

			m_Plane.SetNormalAndPosition(rayOrigin.forward, transform.position);

			m_DragTangent = Vector3.Cross(transform.up, startDragPosition - transform.position);

			UpdateHandleTip(radialEventData);

			base.OnHandleBeginDrag(eventData);
		}

		protected override void OnHandleDrag(HandleEventData eventData)
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
			eventData.deltaRotation = Quaternion.AngleAxis(angle, transform.up);
			
			m_LastPosition = worldPosition;
			m_LastDragForward = rayOrigin.forward;

			if (m_HandleTip != null)
				m_HandleTip.RotateAround(transform.position, transform.up, angle);

			base.OnHandleDrag(eventData);
		}

		protected override void OnHandleEndDrag(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as RadialHandleEventData);

			base.OnHandleEndDrag(eventData);
		}
	}
}