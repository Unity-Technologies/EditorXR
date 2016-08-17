using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class PlaneHandle : BaseHandle, IRayDragHandler
	{
		[SerializeField]
		private Material m_PlaneMaterial;

		private const float kMaxDragDistance = 1000f;

		private Plane m_Plane;
		private Vector3 m_LastPosition;

		public override void OnBeginDrag(RayEventData eventData)
		{
			base.OnBeginDrag(eventData);

			m_LastPosition = eventData.pointerCurrentRaycast.worldPosition;

			m_Plane.SetNormalAndPosition(transform.forward, transform.position);

			OnHandleBeginDrag();
		}

		public void OnDrag(RayEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;

			var worldPosition = m_LastPosition;

			float distance;
			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Min(Mathf.Abs(distance), kMaxDragDistance));

			var delta = worldPosition - m_LastPosition;
			m_LastPosition = worldPosition;

			delta = transform.InverseTransformVector(delta);
			delta.z = 0;
			delta = transform.TransformVector(delta);

			OnHandleDrag(new HandleDragEventData(delta));
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			base.OnEndDrag(eventData);
			
			OnHandleEndDrag();
		}
	}
}