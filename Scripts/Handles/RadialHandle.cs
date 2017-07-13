#if UNITY_EDITOR
using System.Collections.Generic;
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
		readonly Dictionary<Transform, Vector3> m_LastPositions = new Dictionary<Transform, Vector3>(k_DefaultCapacity);
		readonly Dictionary<Transform, Vector3> m_LastDragForwards = new Dictionary<Transform, Vector3>(k_DefaultCapacity);

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new RadialHandleEventData(eventData.rayOrigin, UIUtils.IsDirectEvent(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
		}

		protected override void OnHandleDragStarted(HandleEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;

			var radialEventData = (RadialHandleEventData)eventData;
			m_LastPositions[rayOrigin] = radialEventData.raycastHitWorldPosition;
			m_LastDragForwards[rayOrigin] = rayOrigin.forward;

			m_Plane.SetNormalAndPosition(rayOrigin.forward, transform.position);

			base.OnHandleDragStarted(eventData);
		}

		protected override void OnHandleDragging(HandleEventData eventData)
		{
			var rayOrigin = eventData.rayOrigin;

			var lastPosition = m_LastPositions[rayOrigin];
			var lastDragForward = m_LastDragForwards[rayOrigin];
			var worldPosition = lastPosition;

			float distance;
			var ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Abs(distance));

			var dragTangent = Vector3.Cross(transform.up, (startDragPositions[rayOrigin] - transform.position).normalized);
			var angle = m_TurnSpeed * Vector3.Angle(rayOrigin.forward, lastDragForward) *
				Vector3.Dot((worldPosition - lastPosition).normalized, dragTangent);
			eventData.deltaRotation = Quaternion.AngleAxis(angle, transform.up);
			
			m_LastPositions[rayOrigin] = worldPosition;
			m_LastDragForwards[rayOrigin] = rayOrigin.forward;

			base.OnHandleDragging(eventData);
		}
	}
}
#endif
