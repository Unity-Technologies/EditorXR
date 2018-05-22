
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    sealed class AffordanceTooltipPlacement : MonoBehaviour, ITooltipPlacement
    {
        const FacingDirection k_AllDirections = (FacingDirection)0xFFF;

        [SerializeField]
        Transform m_TooltipTarget;

        [SerializeField]
        Transform m_TooltipSource;

        [SerializeField]
        TextAlignment m_TooltipAlignment = TextAlignment.Center;

        [FlagsProperty]
        [SerializeField]
        FacingDirection m_FacingDirection = k_AllDirections;

        public Transform tooltipTarget { get { return m_TooltipTarget; } }
        public Transform tooltipSource { get { return m_TooltipSource; } }
        public TextAlignment tooltipAlignment { get { return m_TooltipAlignment; } }
        public FacingDirection facingDirection { get { return m_FacingDirection; } }

        void Start()
        {
            if (!m_TooltipTarget)
                m_TooltipTarget = transform;
        }
    }
}

