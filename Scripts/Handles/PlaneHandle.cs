﻿#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
	sealed class PlaneHandle : BaseHandle
	{
		private class PlaneHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public PlaneHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

		[SerializeField]
		private Material m_PlaneMaterial;

		[SerializeField]
		ConstrainedAxis m_Constraints;

		private const float k_MaxDragDistance = 1000f;

		private Plane m_Plane;
		private Vector3 m_LastPosition;

		public ConstrainedAxis constraints { get { return m_Constraints; } }

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new PlaneHandleEventData(eventData.rayOrigin, UIUtils.IsDirectEvent(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
		}

		protected override void OnHandleDragStarted(HandleEventData eventData)
		{
			var planeEventData = eventData as PlaneHandleEventData;
			m_LastPosition = planeEventData.raycastHitWorldPosition;

			m_Plane.SetNormalAndPosition(transform.forward, transform.position);

			base.OnHandleDragStarted(eventData);
		}

		protected override void OnHandleDragging(HandleEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;

			var worldPosition = m_LastPosition;

			float distance;
			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Min(Mathf.Abs(distance), k_MaxDragDistance));

			var deltaPosition = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			deltaPosition = transform.InverseTransformVector(deltaPosition);
			deltaPosition.z = 0;
			deltaPosition = transform.TransformVector(deltaPosition);
			eventData.deltaPosition = deltaPosition;

			base.OnHandleDragging(eventData);
		}
	}
}
#endif
