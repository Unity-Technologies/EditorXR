using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    sealed class TransformCopy : MonoBehaviour
    {
        enum Space
        {
            World,
            Local
        }

        [SerializeField]
        Transform m_SourceTransform;

        [SerializeField]
        float m_XPositionPadding = 0f;

        [SerializeField]
        float m_YPositionPadding = 0f;

        [SerializeField]
        float m_ZPositionPadding = 0f;

        [SerializeField]
        bool m_ParentUnderSource = true;

        [SerializeField]
        Space m_Space;

        [SerializeField]
        bool m_ForceAlwaysUpdate = false;

        Vector3 m_Padding;

        void Awake()
        {
            m_Padding = new Vector3(m_XPositionPadding, m_YPositionPadding, m_ZPositionPadding);
            m_SourceTransform = m_SourceTransform ?? transform.parent;
            if (m_ParentUnderSource)
                transform.SetParent(m_SourceTransform, false);

            DriveTransform();
        }

        void Update()
        {
            if (gameObject.activeInHierarchy && (m_ForceAlwaysUpdate || m_SourceTransform.hasChanged))
                DriveTransform();
        }

        void DriveTransform()
        {
            if (m_Space == Space.World)
                transform.position = m_SourceTransform.TransformPoint(m_Padding);
            else
                transform.localPosition = m_SourceTransform.localPosition + m_Padding;
        }
    }
}
