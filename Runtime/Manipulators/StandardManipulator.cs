using Unity.EditorXR.Handles;
using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Utilities;
using UnityEngine;

namespace Unity.EditorXR.Manipulators
{
    sealed class StandardManipulator : BaseManipulator
    {
#pragma warning disable 649
        [SerializeField]
        Transform m_PlaneHandlesParent;

        [SerializeField]
        Mesh m_RadialHandleMesh;

        [SerializeField]
        Mesh m_FatRadialHandleMesh;

        [SerializeField]
        float m_SphereHandleHideScale = 0.1f;
#pragma warning restore 649

        void Update()
        {
            if (!dragging)
            {
                // Place the plane handles in a good location that is accessible to the user
                var viewerPosition = CameraUtils.GetMainCamera().transform.position;
                var childCount = m_PlaneHandlesParent.childCount;
                for (var i = 0; i < childCount; i++)
                {
                    var t = m_PlaneHandlesParent.GetChild(i);
                    var localPos = t.localPosition;
                    localPos.x = Mathf.Abs(localPos.x) * (transform.position.x < viewerPosition.x ? 1 : -1);
                    localPos.y = Mathf.Abs(localPos.y) * (transform.position.y < viewerPosition.y ? 1 : -1);
                    localPos.z = Mathf.Abs(localPos.z) * (transform.position.z < viewerPosition.z ? 1 : -1);
                    t.localPosition = localPos;
                }
            }
        }

        protected override void ShowHoverState(BaseHandle handle, bool hovering)
        {
            base.ShowHoverState(handle, hovering);

            if (handle is RadialHandle)
                handle.GetComponent<MeshFilter>().sharedMesh = hovering ? m_FatRadialHandleMesh : m_RadialHandleMesh;
        }

        protected override void OnHandlePointerDown(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHandlePointerDown(handle, eventData);

            if (handle.IndexOfDragSource(eventData.rayOrigin) > 0)
                return;

            if (handle is SphereHandle)
                handle.transform.localScale *= m_SphereHandleHideScale;
        }

        protected override void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHandleDragging(handle, eventData);

            var rayOrigin = eventData.rayOrigin;
            if (handle.IndexOfDragSource(rayOrigin) > 0)
                return;

            if (handle is RadialHandle)
            {
                if (rotate != null)
                    rotate(eventData.deltaRotation, rayOrigin);
            }
            else
            {
                AxisFlags constraints = 0;
                var constrainedHandle = handle as IAxisConstraints;
                if (constrainedHandle != null)
                    constraints = constrainedHandle.constraints;

                if (translate != null)
                    translate(eventData.deltaPosition, rayOrigin, constraints);
            }
        }

        protected override void OnHandlePointerUp(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHandlePointerUp(handle, eventData);

            if (handle.IndexOfDragSource(eventData.rayOrigin) > 0)
                return;

            if (handle is SphereHandle)
                handle.transform.localScale /= m_SphereHandleHideScale;
        }
    }
}
