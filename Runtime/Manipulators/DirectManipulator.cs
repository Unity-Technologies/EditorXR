using System;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Manipulators
{
    sealed class DirectManipulator : MonoBehaviour, IManipulator
    {
        public Transform target
        {
            set { m_Target = value; }
        }

        [SerializeField]
        Transform m_Target;

        [SerializeField]
        List<BaseHandle> m_AllHandles = new List<BaseHandle>();

        Vector3 m_PositionOffset;
        Quaternion m_RotationOffset;

        public Action<Vector3, Transform, AxisFlags> translate { private get; set; }
        public Action<Quaternion, Transform> rotate { private get; set; }
        public Action<Vector3> scale { private get; set; }

        public bool dragging { get; private set; }
        public event Action dragStarted;
        public event Action<Transform> dragEnded;

        void OnEnable()
        {
            foreach (var h in m_AllHandles)
            {
                h.dragStarted += OnHandleDragStarted;
                h.dragging += OnHandleDragging;
                h.dragEnded += OnHandleDragEnded;
            }
        }

        void OnDisable()
        {
            foreach (var h in m_AllHandles)
            {
                h.dragStarted -= OnHandleDragStarted;
                h.dragging -= OnHandleDragging;
                h.dragEnded -= OnHandleDragEnded;
            }
        }

        void OnHandleDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            foreach (var h in m_AllHandles)
            {
                h.gameObject.SetActive(h == handle);
            }
            dragging = true;

            var target = m_Target == null ? transform : m_Target;

            var rayOrigin = eventData.rayOrigin;
            var inverseRotation = Quaternion.Inverse(rayOrigin.rotation);
            m_PositionOffset = inverseRotation * (target.transform.position - rayOrigin.position);
            m_RotationOffset = inverseRotation * target.transform.rotation;

            if (dragStarted != null)
                dragStarted();
        }

        void OnHandleDragging(BaseHandle handle, HandleEventData eventData)
        {
            var target = m_Target == null ? transform : m_Target;

            var rayOrigin = eventData.rayOrigin;
            translate(rayOrigin.position + rayOrigin.rotation * m_PositionOffset - target.position, rayOrigin, 0);
            rotate(Quaternion.Inverse(target.rotation) * rayOrigin.rotation * m_RotationOffset, rayOrigin);
        }

        void OnHandleDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            if (gameObject.activeSelf)
            {
                foreach (var h in m_AllHandles)
                {
                    h.gameObject.SetActive(true);
                }
            }

            dragging = false;

            if (dragEnded != null)
                dragEnded(eventData.rayOrigin);
        }
    }
}
