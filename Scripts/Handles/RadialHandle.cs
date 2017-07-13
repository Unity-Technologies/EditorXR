#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
	sealed class RadialHandle : BaseHandle
	{
		internal class RadialHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public RadialHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

		[SerializeField]
		float m_TurnSpeed;

		Plane m_Plane;
		Vector3 m_LastPosition;
		Vector3 m_LastDragForward;

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new RadialHandleEventData(eventData.rayOrigin, UIUtils.IsDirectEvent(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
		}

		protected override void OnHandleDragStarted(HandleEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;

			var radialEventData = (RadialHandleEventData)eventData;
			m_LastPosition = radialEventData.raycastHitWorldPosition;
			m_LastDragForward = rayOrigin.forward;

			m_Plane.SetNormalAndPosition(rayOrigin.forward, transform.position);

			base.OnHandleDragStarted(eventData);
		}

		protected override void OnHandleDragging(HandleEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;

			var worldPosition = m_LastPosition;

			float distance;
			var ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Abs(distance));

			var dragTangent = Vector3.Cross(transform.up, (startDragPositions[rayOrigin] - transform.position).normalized);
			var angle = m_TurnSpeed * Vector3.Angle(rayOrigin.forward, m_LastDragForward) *
				Vector3.Dot((worldPosition - m_LastPosition).normalized, dragTangent);
			eventData.deltaRotation = Quaternion.AngleAxis(angle, transform.up);
			
			m_LastPosition = worldPosition;
			m_LastDragForward = rayOrigin.forward;

			base.OnHandleDragging(eventData);
		}
	}
}
#endif
