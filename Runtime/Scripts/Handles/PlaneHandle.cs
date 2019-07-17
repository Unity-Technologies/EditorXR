using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils.GUI;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Handles
{
    sealed class PlaneHandle : BaseHandle, IAxisConstraints, IUsesViewerScale
    {
        const float k_MaxDragDistance = 1000f;

        class PlaneHandleEventData : HandleEventData
        {
            public Vector3 raycastHitWorldPosition;

            public PlaneHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) {}
        }

#pragma warning disable 649
        [FlagsProperty]
        [SerializeField]
        AxisFlags m_Constraints;
#pragma warning restore 649

        Plane m_Plane;
        readonly Dictionary<Transform, Vector3> m_LastPositions = new Dictionary<Transform, Vector3>(k_DefaultCapacity);

        public AxisFlags constraints { get { return m_Constraints; } }

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        static readonly PlaneHandleEventData k_LinearHandleEventData = new PlaneHandleEventData(null, false);

        protected override HandleEventData GetHandleEventData(RayEventData eventData)
        {
            k_LinearHandleEventData.rayOrigin = eventData.rayOrigin;
            k_LinearHandleEventData.camera = eventData.camera;
            k_LinearHandleEventData.position = eventData.position;
            k_LinearHandleEventData.direct = UIUtils.IsDirectEvent(eventData);
            k_LinearHandleEventData.raycastHitWorldPosition = eventData.pointerCurrentRaycast.worldPosition;

            return k_LinearHandleEventData;
        }

        protected override void OnHandleDragStarted(HandleEventData eventData)
        {
            var planeEventData = eventData as PlaneHandleEventData;
            m_LastPositions[eventData.rayOrigin] = planeEventData.raycastHitWorldPosition;

            m_Plane.SetNormalAndPosition(transform.forward, transform.position);

            base.OnHandleDragStarted(eventData);
        }

        protected override void OnHandleDragging(HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;

            var lastPosition = m_LastPositions[eventData.rayOrigin];
            var worldPosition = lastPosition;

            var ray = eventData.camera == null ?
                new Ray(rayOrigin.position, rayOrigin.forward) :
                eventData.camera.ScreenPointToRay(eventData.position);

            float distance;
            if (m_Plane.Raycast(ray, out distance))
                worldPosition = ray.GetPoint(Mathf.Min(Mathf.Abs(distance), k_MaxDragDistance * this.GetViewerScale()));

            var deltaPosition = worldPosition - lastPosition;
            m_LastPositions[eventData.rayOrigin] = worldPosition;

            deltaPosition = transform.InverseTransformVector(deltaPosition);
            deltaPosition.z = 0;
            deltaPosition = transform.TransformVector(deltaPosition);
            eventData.deltaPosition = deltaPosition;

            base.OnHandleDragging(eventData);
        }
    }
}
