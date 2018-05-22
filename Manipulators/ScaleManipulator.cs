
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators
{
    sealed class ScaleManipulator : BaseManipulator
    {
        [SerializeField]
        BaseHandle m_UniformHandle;

        protected override void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
        {
            base.OnHandleDragging(handle, eventData);

            if (handle == m_UniformHandle)
            {
                scale(Vector3.one * eventData.deltaPosition.y / transform.localScale.x);
            }
            else
            {
                var handleTransform = handle.transform;
                var inverseRotation = Quaternion.Inverse(handleTransform.rotation);

                var localStartDragPosition = inverseRotation
                    * (handle.startDragPositions[eventData.rayOrigin] - handleTransform.position);
                var delta = (inverseRotation * eventData.deltaPosition).z / localStartDragPosition.z;
                scale(Quaternion.Inverse(transform.rotation) * handleTransform.forward * delta);
            }
        }

        protected override void UpdateHandleTip(BaseHandle handle, HandleEventData eventData, bool active)
        {
            if (handle == m_UniformHandle)
                return;

            base.UpdateHandleTip(handle, eventData, active);
        }
    }
}

