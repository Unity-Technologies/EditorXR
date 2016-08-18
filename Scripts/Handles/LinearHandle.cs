using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class LinearHandle : BaseHandle, IRayDragHandler, IRayHoverHandler
	{
		[SerializeField]
		private Transform m_HandleTip;

		private const float kMaxDragDistance = 1000f;

		private Plane m_Plane;
		private Vector3 m_LastPosition;

		private void OnDisable()
		{
			if (m_HandleTip != null)
				m_HandleTip.gameObject.SetActive(false);
		}

		public void OnRayHover(RayEventData eventData)
		{
			UpdateHandleTip(eventData);
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

		private void UpdateHandleTip(RayEventData eventData)
		{
			if (m_HandleTip != null)
			{
				m_HandleTip.gameObject.SetActive(m_Hovering || m_Dragging);

				if (m_Hovering || m_Dragging) // Reposition handle tip based on current raycast position when hovering or dragging
				{
					if (eventData != null)
						m_HandleTip.position =
							transform.TransformPoint(new Vector3(0, 0,
								transform.InverseTransformPoint(eventData.pointerCurrentRaycast.worldPosition).z));
				}
			}
		}

		public override void OnBeginDrag(RayEventData eventData)
		{
			base.OnBeginDrag(eventData);

			m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

			// Create a plane through the axis that rotates to avoid being parallel to the ray, so that you can prevent
			// intersections at infinity
			var forward = transform.InverseTransformVector(eventData.rayOrigin.forward);
			forward.z = 0;			
			m_Plane.SetNormalAndPosition(transform.TransformVector(forward), transform.position);

			UpdateHandleTip(eventData);

			OnHandleBeginDrag(new HandleDragEventData(eventData.rayOrigin));
		}

		public void OnDrag(RayEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;
			Vector3 worldPosition = m_LastPosition;

			// Continue to rotate plane, so that the ray direction isn't parallel to the plane
			var forward = transform.InverseTransformVector(rayOrigin.forward);
			forward.z = 0;
			m_Plane.normal = transform.TransformVector(forward);

			float distance = 0f;
			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Min(Mathf.Abs(distance), kMaxDragDistance));

			var delta = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			delta = transform.InverseTransformVector(delta);
			delta.x = 0;
			delta.y = 0;
			delta = transform.TransformVector(delta);

			UpdateHandleTip(eventData);

			OnHandleDrag(new HandleDragEventData(delta, rayOrigin));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);

			UpdateHandleTip(eventData);

			OnHandleEndDrag(new HandleDragEventData(eventData.rayOrigin));
		}
	}
}