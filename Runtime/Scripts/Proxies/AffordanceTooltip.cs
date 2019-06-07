using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    sealed class AffordanceTooltip : MonoBehaviour, ITooltip, ITooltipPlacement
    {
        [SerializeField]
        string m_TooltipText;

        [SerializeField]
        AffordanceTooltipPlacement[] m_Placements;

        FacingDirection m_LastFacingDirection;

        public Transform tooltipTarget { get { return GetPlacement(m_LastFacingDirection).tooltipTarget; } }
        public Transform tooltipSource { get { return GetPlacement(m_LastFacingDirection).tooltipSource; } }
        public TextAlignment tooltipAlignment { get { return GetPlacement(m_LastFacingDirection).tooltipAlignment; } }

        public string tooltipText
        {
            get { return m_TooltipText; }
            set { m_TooltipText = value; }
        }

        internal AffordanceTooltipPlacement[] placements { set { m_Placements = value; } }

        public AffordanceTooltipPlacement GetPlacement(FacingDirection direction)
        {
            m_LastFacingDirection = direction;

            foreach (var placement in m_Placements)
            {
                if ((placement.facingDirection & direction) != 0)
                    return placement;
            }

            Debug.LogWarning(string.Format("No placement matching {0} found in {1}", direction, this), this);
            return null;
        }
    }
}
