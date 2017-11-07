#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    sealed class TransformCopy : MonoBehaviour
    {
        enum Space
        {
            Local,
            World
        }

        Transform m_TargetTransform;

        [SerializeField]
        Transform m_SourceTransform;

        [SerializeField]
        float m_XPositionPadding = 0.005f;

        [SerializeField]
        float m_YPositionPadding = 0f;

        [SerializeField]
        float m_ZPositionPadding = 0.00055f;

        [SerializeField]
        float m_XScalePadding = 0.01f;

        [SerializeField]
        float m_YScalePadding = 0f;

        [SerializeField]
        bool m_ParentUnderSource = true;

        [SerializeField]
        bool m_CopyScale = true;

        [SerializeField]
        Space m_Space;

        void Awake()
        {
            m_TargetTransform = transform;

            if (m_ParentUnderSource)
                m_TargetTransform.SetParent(m_SourceTransform, false);

            DriveTransformWithRectTransform();
        }

        void Update()
        {
            if (m_SourceTransform.hasChanged)
                DriveTransformWithRectTransform();
        }

        void DriveTransformWithRectTransform()
        {
            if (!m_SourceTransform || !m_TargetTransform || !gameObject.activeInHierarchy)
                return;

            if (m_Space == Space.Local)
            {
                var localPosition = m_SourceTransform.localPosition;
                /*
                m_TargetTransform.localPosition = new Vector3(pivotOffset.x + m_XPositionPadding, pivotOffset.y + m_YPositionPadding, m_ZPositionPadding);

                if (m_CopyScale)
                    m_TargetTransform.localScale = new Vector3(rectSize.x + m_XScalePadding, rectSize.y + m_YScalePadding, transform.localScale.z);
                    */
            }
            else
            {
                var worldPosition = m_SourceTransform.position;
                m_TargetTransform.position = new Vector3(worldPosition.x + m_XPositionPadding, worldPosition.y + m_YPositionPadding, worldPosition.z + m_ZPositionPadding);

                /*
                if (m_CopyScale)
                    m_TargetTransform.localScale = new Vector3(rectSize.x + m_XScalePadding, rectSize.y + m_YScalePadding, transform.localScale.z);
              */
            }
        }
    }
}
#endif
