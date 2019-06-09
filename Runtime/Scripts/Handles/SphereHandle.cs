using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Handles
{
    sealed class SphereHandle : BaseHandle, IScrollHandler, IUsesViewerScale
    {
        const float k_MaxSphereRadius = 1000f;

        class SphereHandleEventData : HandleEventData
        {
            public float raycastHitDistance;

            public SphereHandleEventData(Transform rayOrigin, bool direct) : base(rayOrigin, direct) {}
        }

        const float k_InitialScrollRate = 2f;
        const float k_ScrollAcceleration = 14f;

        const float k_DistanceScale = 0.1f;

        float m_ScrollRate;
        Vector3 m_LastPosition;
        float m_CurrentRadius;

#if !FI_AUTOFILL
        IProvidesViewerScale IFunctionalitySubscriber<IProvidesViewerScale>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        static readonly SphereHandleEventData k_SphereHandleEventData = new SphereHandleEventData(null, false);

        protected override HandleEventData GetHandleEventData(RayEventData eventData)
        {
            k_SphereHandleEventData.rayOrigin = eventData.rayOrigin;
            k_SphereHandleEventData.direct = UIUtils.IsDirectEvent(eventData);
            k_SphereHandleEventData.raycastHitDistance = eventData.pointerCurrentRaycast.distance;

            return k_SphereHandleEventData;
        }

        protected override void OnHandleDragStarted(HandleEventData eventData)
        {
            var sphereEventData = (SphereHandleEventData)eventData;

            var rayOrigin = eventData.rayOrigin;
            if (IndexOfDragSource(rayOrigin) == 0)
            {
                m_CurrentRadius = sphereEventData.raycastHitDistance;
                m_ScrollRate = k_InitialScrollRate;
                m_LastPosition = GetRayPoint(eventData);

                base.OnHandleDragStarted(eventData);
            }
        }

        protected override void OnHandleDragging(HandleEventData eventData)
        {
            if (IndexOfDragSource(eventData.rayOrigin) == 0)
            {
                var worldPosition = GetRayPoint(eventData);
                eventData.deltaPosition = worldPosition - m_LastPosition;
                m_LastPosition = worldPosition;

                base.OnHandleDragging(eventData);
            }
        }

        protected override void OnHandleDragEnded(HandleEventData eventData)
        {
            if (!hasDragSource)
                base.OnHandleDragEnded(eventData);
        }

        public void ChangeRadius(float delta)
        {
            var distance = Vector3.Distance(CameraUtils.GetMainCamera().transform.position, transform.position);
            m_CurrentRadius += delta * distance * k_DistanceScale;
            m_CurrentRadius = Mathf.Clamp(m_CurrentRadius, 0f, k_MaxSphereRadius * this.GetViewerScale());
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (m_DragSources.Count == 0)
                return;

            // Scrolling changes the radius of the sphere while dragging, and accelerates
            if (Mathf.Abs(eventData.scrollDelta.y) > 0.5f)
                m_ScrollRate += Mathf.Abs(eventData.scrollDelta.y) * k_ScrollAcceleration * Time.deltaTime;
            else
                m_ScrollRate = k_InitialScrollRate;

            ChangeRadius(m_ScrollRate * eventData.scrollDelta.y * Time.deltaTime);
        }

        Vector3 GetRayPoint(HandleEventData eventData)
        {
            var rayOrigin = eventData.rayOrigin;
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            return ray.GetPoint(m_CurrentRadius);
        }
    }
}
