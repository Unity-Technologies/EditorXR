using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class SphereHandle : BaseHandle, IRayDragHandler, IScrollHandler
	{
		private const float kInitialScrollRate = 2f;
		private const float kScrollAcceleration = 14f;
		
		private float m_ScrollRate;
		private Vector3 m_LastPosition;
		private float m_CurrentRadius;

		public override void OnBeginDrag(RayEventData eventData)
		{
			base.OnBeginDrag(eventData);

			m_CurrentRadius = eventData.pointerCurrentRaycast.distance;

			Ray ray = EventDataToRay(eventData);
			m_LastPosition = ray.GetPoint(m_CurrentRadius);

			m_ScrollRate = kInitialScrollRate;

			OnHandleBeginDrag(new HandleDragEventData(eventData.rayOrigin));
		}

		public void OnDrag(RayEventData eventData)
		{
			Ray ray = EventDataToRay(eventData);
			var worldPosition = ray.GetPoint(m_CurrentRadius);

			var deltaPos = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			OnHandleDrag(new HandleDragEventData(deltaPos, eventData.rayOrigin));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);

			OnHandleEndDrag(new HandleDragEventData(eventData.rayOrigin));
		}

		public void ChangeRadius(float delta)
		{
			m_CurrentRadius += delta;
			m_CurrentRadius = Mathf.Max(m_CurrentRadius, 0f);
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (!m_Dragging)
				return;

			// Scolling changes the radius of the sphere while dragging, and accelerates
			if (Mathf.Abs(eventData.scrollDelta.y) > 0.5f)
				m_ScrollRate += Mathf.Abs(eventData.scrollDelta.y)*kScrollAcceleration*Time.unscaledDeltaTime;
			else
				m_ScrollRate = kInitialScrollRate;

			ChangeRadius(m_ScrollRate*eventData.scrollDelta.y*Time.unscaledDeltaTime);
		}

		private Ray EventDataToRay(RayEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;
			return new Ray(rayOrigin.position, rayOrigin.forward);
		}
	}
}