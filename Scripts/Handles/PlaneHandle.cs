using UnityEngine.EventSystems;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Handles
{
	public class PlaneHandle : BaseHandle
	{
		private class PlaneHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public PlaneHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

		[SerializeField]
		private Material m_PlaneMaterial;

		private const float kMaxDragDistance = 1000f;

		private Plane m_Plane;
		private Vector3 m_LastPosition;

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new PlaneHandleEventData(eventData.rayOrigin, U.UI.IsDirectEvent(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
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
				worldPosition = ray.GetPoint(Mathf.Min(Mathf.Abs(distance), kMaxDragDistance));

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