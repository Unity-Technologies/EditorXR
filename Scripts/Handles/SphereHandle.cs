using UnityEngine;
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

			Ray ray = new Ray(eventData.rayOrigin.position, eventData.rayOrigin.forward);
			m_LastPosition = ray.GetPoint(m_CurrentRadius);
			m_LastRotation = eventData.rayOrigin.rotation;
			m_ScrollRate = kInitialScrollRate;
			OnHandleBeginDrag();
		}

		public void OnDrag(RayEventData eventData)
		{
			var worldPosition = m_LastPosition;

			Ray ray = new Ray(eventData.rayOrigin.position, eventData.rayOrigin.forward);
			worldPosition = ray.GetPoint(m_CurrentRadius);

			var deltaPos = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			var deltaRot = Quaternion.Inverse(m_LastRotation) * eventData.rayOrigin.rotation;
			m_LastRotation = eventData.rayOrigin.rotation;

			OnHandleDrag(new HandleDragEventData(deltaPos, deltaRot));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);

			OnHandleEndDrag();
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