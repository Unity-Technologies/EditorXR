using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.Utils.GUI;
using UnityEngine;

namespace Unity.Labs.EditorXR.Proxies
{
    sealed class AffordanceTooltipPlacement : MonoBehaviour, ITooltipPlacement
    {
        const FacingDirection k_AllDirections = (FacingDirection)0xFFF;

#pragma warning disable 649
        [SerializeField]
        Transform m_TooltipTarget;

        [SerializeField]
        Transform m_TooltipSource;

        [SerializeField]
        TextAlignment m_TooltipAlignment = TextAlignment.Center;

        [FlagsProperty]
        [SerializeField]
        FacingDirection m_FacingDirection = k_AllDirections;
#pragma warning restore 649

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
