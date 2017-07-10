#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
	sealed class PlaneHandle : BaseHandle, IAxisConstraints
	{
		const float k_MaxDragDistance = 1000f;
		const float k_ScaleBump = 1.1f;

		class PlaneHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public PlaneHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

		[SerializeField]
		Material m_PlaneMaterial;

		[SerializeField]
		bool m_ScaleBump;

		[FlagsProperty]
		[SerializeField]
		ConstrainedAxis m_Constraints;

		Plane m_Plane;
		Vector3 m_LastPosition;

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
			var rayOrigin = eventData.rayOrigin;

			var worldPosition = m_LastPosition;

			float distance;
			var ray = new Ray(rayOrigin.position, rayOrigin.forward);
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

		protected override void OnHandleHoverStarted(HandleEventData eventData) {
			if (m_ScaleBump)
				transform.localScale *= k_ScaleBump;

			base.OnHandleHoverStarted(eventData);
		}

		protected override void OnHandleHoverEnded(HandleEventData eventData) {
			if (m_ScaleBump)
				transform.localScale /= k_ScaleBump;

			base.OnHandleHoverStarted(eventData);
		}
	}
}
#endif
