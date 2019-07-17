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

            public RadialHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) {}
        }

#pragma warning disable 649
        [SerializeField]
        float m_TurnSpeed;
#pragma warning restore 649

        Plane m_Plane;
        readonly Dictionary<Transform, Vector3> m_LastPositions = new Dictionary<Transform, Vector3>(k_DefaultCapacity);
        readonly Dictionary<Transform, Vector3> m_LastDragForwards = new Dictionary<Transform, Vector3>(k_DefaultCapacity);

        // Local method use only -- created here to reduce garbage collection
        static readonly RadialHandleEventData k_RadialHandleEventData = new RadialHandleEventData(null, false);

        protected override HandleEventData GetHandleEventData(RayEventData eventData)
        {
            k_RadialHandleEventData.rayOrigin = eventData.rayOrigin;
            k_RadialHandleEventData.camera = eventData.camera;
            k_RadialHandleEventData.position = eventData.position;
            k_RadialHandleEventData.direct = UIUtils.IsDirectEvent(eventData);
            k_RadialHandleEventData.raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition;

            return k_RadialHandleEventData;
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
