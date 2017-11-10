#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    sealed class TransformCopy : MonoBehaviour, IUsesViewerScale
    {
        enum Space
        {
            World,
            Local
        }

        [SerializeField]
        Transform m_SourceTransform;

        [SerializeField]
        float m_XPositionPadding = 0.005f;

        [SerializeField]
        float m_YPositionPadding = 0f;

        [SerializeField]
        float m_ZPositionPadding = 0.00055f;

        [SerializeField]
        bool m_ParentUnderSource = true;

        [SerializeField]
        Space m_Space;

        [SerializeField]
        bool m_ForceAlwaysUpdate = false;

        void Awake()
        {
            m_SourceTransform = m_SourceTransform ?? transform.parent;
            if (m_ParentUnderSource)
                transform.SetParent(m_SourceTransform, false);

            DriveTransformWithRectTransform();
        }

        void Update()
        {
            if (gameObject.activeInHierarchy && (m_ForceAlwaysUpdate || m_SourceTransform.hasChanged))
                DriveTransformWithRectTransform();
        }

        void DriveTransformWithRectTransform()
        {
            var viewerScale = this.GetViewerScale();
            if (m_Space == Space.World)
            {
                var sourceWorldPosition = m_SourceTransform.position;
                sourceWorldPosition = new Vector3(sourceWorldPosition.x + m_XPositionPadding * viewerScale, sourceWorldPosition.y + m_YPositionPadding * viewerScale, sourceWorldPosition.z + m_ZPositionPadding * viewerScale);
                transform.position = sourceWorldPosition;
            }
            else
            {
                var localPosition = m_SourceTransform.localPosition;
                localPosition = new Vector3(localPosition.x + m_XPositionPadding * viewerScale, localPosition.y + m_YPositionPadding * viewerScale, localPosition.z + m_ZPositionPadding * viewerScale);
                transform.position = localPosition;
            }
        }
    }
}
#endif
