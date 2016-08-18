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
		private Quaternion m_LastRotation;
		private float m_CurrentRadius = 0f;

		public override void OnBeginDrag(RayEventData eventData)
		{
			base.OnBeginDrag(eventData);

			m_CurrentRadius = eventData.pointerCurrentRaycast.distance;

			var rayOrigin = eventData.rayOrigin;

			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			m_LastPosition = ray.GetPoint(m_CurrentRadius);
			m_LastRotation = rayOrigin.rotation;
			m_ScrollRate = kInitialScrollRate;
			OnHandleBeginDrag(new HandleDragEventData(rayOrigin));
		}

		public void OnDrag(RayEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;

			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			var worldPosition = ray.GetPoint(m_CurrentRadius);

			var deltaPos = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			var deltaRot = Quaternion.Inverse(m_LastRotation) * rayOrigin.rotation;
			m_LastRotation = rayOrigin.rotation;

			OnHandleDrag(new HandleDragEventData(deltaPos, deltaRot, rayOrigin));
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
	}
}