
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
    sealed class LinearHandle : BaseHandle, IAxisConstraints, IUsesViewerScale
    {
        const float k_MaxDragDistance = 1000f;

        internal class LinearHandleEventData : HandleEventData
        {
            public Vector3 raycastHitWorldPosition;

            public LinearHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) {}
        }

        [SerializeField]
        bool m_OrientDragPlaneToRay = true;

        [FlagsProperty]
        [SerializeField]
        AxisFlags m_Constraints;

        readonly Dictionary<Transform, Vector3> m_LastPositions = new Dictionary<Transform, Vector3>(k_DefaultCapacity);

        Plane m_Plane;

        public AxisFlags constraints { get { return m_Constraints; } }

        // Local method use only -- created here to reduce garbage collection
        static readonly LinearHandleEventData k_LinearHandleEventData = new LinearHandleEventData(null, false);

        protected override HandleEventData GetHandleEventData(RayEventData eventData)
        {
            k_LinearHandleEventData.rayOrigin = eventData.rayOrigin;
            k_LinearHandleEventData.direct = UIUtils.IsDirectEvent(eventData);
            k_LinearHandleEventData.raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition;

            return k_LinearHandleEventData;
        }

        void UpdateEventData(LinearHandleEventData eventData, bool setLastPosition = true)
        {
            var rayOrigin = eventData.rayOrigin;
            var lastPosition = m_LastPositions[rayOrigin];
            var worldPosition = lastPosition;

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

            float distance;
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (m_Plane.Raycast(ray, out distance))
                worldPosition = ray.GetPoint(Mathf.Min(distance, k_MaxDragDistance * this.GetViewerScale()));

            eventData.raycastHitWorldPosition = worldPosition;

            eventData.deltaPosition = Vector3.Project(worldPosition - lastPosition, transform.forward);

            if (setLastPosition)
                m_LastPositions[rayOrigin] = worldPosition;
        }

        protected override void OnHandleHoverStarted(HandleEventData eventData)
        {
            var linearEventData = (LinearHandleEventData)eventData;

            m_LastPositions[eventData.rayOrigin] = linearEventData.raycastHitWorldPosition;

            if (m_DragSources.Count == 0)
                UpdateEventData(linearEventData);

            base.OnHandleHoverStarted(eventData);
        }

        protected override void OnHandleHovering(HandleEventData eventData)
        {
            if (m_DragSources.Count == 0)
                UpdateEventData((LinearHandleEventData)eventData);

            base.OnHandleHovering(eventData);
        }

        protected override void OnHandleDragStarted(HandleEventData eventData)
        {
            var linearEventData = (LinearHandleEventData)eventData;
            m_LastPositions[eventData.rayOrigin] = linearEventData.raycastHitWorldPosition;
            UpdateEventData(linearEventData);

            base.OnHandleDragStarted(eventData);
        }

        protected override void OnHandleDragging(HandleEventData eventData)
        {
            UpdateEventData((LinearHandleEventData)eventData);

            base.OnHandleDragging(eventData);
        }
    }
}

