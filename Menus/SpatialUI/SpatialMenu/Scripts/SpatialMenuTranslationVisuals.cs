#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    public class SpatialMenuTranslationVisuals : MonoBehaviour
    {
        [SerializeField]
        Transform m_UpArrow;

        [SerializeField]
        Transform m_DownArrow;

        [SerializeField]
        Transform m_LeftArrow;

        [SerializeField]
        Transform m_RightArrow;

        public bool leftArrowHighlighted
        {
            set
            {
                m_LeftArrow.localScale = value ? Vector3.one * 3f : Vector3.one;
            }
        }

        public bool rightArrowHighlighted
        {
            set
            {
                m_RightArrow.localScale = value ? Vector3.one * 3f : Vector3.one;
            }
        }

        public bool upArrowHighlighted
        {
            set
            {
                m_UpArrow.localScale = value ? Vector3.one * 3f : Vector3.one;
            }
        }

        public bool downArrowHighlighted
        {
            set
            {
                m_DownArrow.localScale = value ? Vector3.one * 3f : Vector3.one;
            }
        }
    }
}
#endif
