#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    public sealed class AffordanceTooltip : MonoBehaviour, ITooltip
    {
        [SerializeField]
        string m_TooltipText;

        [SerializeField]
        AffordanceTooltipPlacement[] m_Placements;

        public string tooltipText
        {
            get { return m_TooltipText; }
            set { m_TooltipText = value; }
        }

        internal AffordanceTooltipPlacement[] placements { set { m_Placements = value; } }

        public AffordanceTooltipPlacement GetPlacement(FacingDirection direction)
        {
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
#endif
