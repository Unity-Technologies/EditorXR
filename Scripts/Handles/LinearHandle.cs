#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
	sealed class LinearHandle : BaseHandle
	{
		class LinearHandleEventData : HandleEventData
		{
			public Vector3 raycastHitWorldPosition;

			public LinearHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) { }
		}

		[SerializeField]
		private Transform m_HandleTip;

		[SerializeField]
		bool m_OrientDragPlaneToRay = true;

		private const float k_MaxDragDistance = 1000f;

		private Plane m_Plane;
		private Vector3 m_LastPosition;

		private void OnDisable()
		{
			if (m_HandleTip != null)
				m_HandleTip.gameObject.SetActive(false);
		}

		protected override HandleEventData GetHandleEventData(RayEventData eventData)
		{
			return new LinearHandleEventData(eventData.rayOrigin, UIUtils.IsDirectEvent(eventData)) { raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition };
		}

		protected override void OnHandleHovering(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as LinearHandleEventData);
		}

		protected override void OnHandleHoverStarted(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as LinearHandleEventData);
			base.OnHandleHoverStarted(eventData);
		}

		protected override void OnHandleHoverEnded(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as LinearHandleEventData);
			base.OnHandleHoverEnded(eventData);
		}

		private void UpdateHandleTip(LinearHandleEventData eventData)
		{
			if (m_HandleTip != null)
			{
				m_HandleTip.gameObject.SetActive(m_HoverSources.Count > 0 || m_DragSources.Count > 0);

				if (m_HoverSources.Count > 0 || m_DragSources.Count > 0) // Reposition handle tip based on current raycast position when hovering or dragging
				{
					if (eventData != null)
						m_HandleTip.position =
							transform.TransformPoint(new Vector3(0, 0,
								transform.InverseTransformPoint(eventData.raycastHitWorldPosition).z));
				}
			}
		}

		void UpdatePlaneOrientation(Transform rayOrigin)
		{
			if (m_OrientDragPlaneToRay)
			{
				// Orient a plane for dragging purposes through the axis that rotates to avoid being parallel to the ray, 
				// so that you can prevent intersections at infinity
				var forward = Quaternion.Inverse(transform.rotation) * (rayOrigin.position - transform.position);
				forward.z = 0;
				m_Plane.SetNormalAndPosition(transform.rotation * forward.normalized, transform.position);
			}
			else
			{
				m_Plane.SetNormalAndPosition(transform.up, transform.position);
			}
		}

		protected override void OnHandleDragStarted(HandleEventData eventData)
		{
			var linearEventData = eventData as LinearHandleEventData;
			m_LastPosition = linearEventData.raycastHitWorldPosition;

			UpdatePlaneOrientation(eventData.rayOrigin);
			UpdateHandleTip(linearEventData);

			base.OnHandleDragStarted(eventData);
		}

		protected override void OnHandleDragging(HandleEventData eventData)
		{
			Transform rayOrigin = eventData.rayOrigin;
			Vector3 worldPosition = m_LastPosition;

			UpdatePlaneOrientation(rayOrigin);

			float distance = 0f;
			Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
			if (m_Plane.Raycast(ray, out distance))
				worldPosition = ray.GetPoint(Mathf.Min(distance, k_MaxDragDistance));

			var linearEventData = eventData as LinearHandleEventData;
			linearEventData.raycastHitWorldPosition = worldPosition;

			eventData.deltaPosition = Vector3.Project(worldPosition - m_LastPosition, transform.forward);

			m_LastPosition = worldPosition;

			UpdateHandleTip(linearEventData);

			base.OnHandleDragging(eventData);
		}

		protected override void OnHandleDragEnded(HandleEventData eventData)
		{
			UpdateHandleTip(eventData as LinearHandleEventData);

			base.OnHandleDragEnded(eventData);
		}
	}
}
#endif
