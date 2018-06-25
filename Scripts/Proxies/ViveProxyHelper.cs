#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    class ViveProxyHelper : MonoBehaviour
    {
        [Serializable]
        public class AffordanceTooltipPlacementOverride
        {
            [SerializeField]
            AffordanceTooltip m_Tooltip;

            [SerializeField]
            AffordanceTooltipPlacement[] m_Placements;

            public AffordanceTooltip tooltip { get { return m_Tooltip; } }
            public AffordanceTooltipPlacement[] placements { get { return m_Placements; } }
        }

        [SerializeField]
        AffordanceTooltipPlacementOverride[] m_LeftPlacementOverrides;

        /// <summary>
        /// Tooltip placement arrays to be replaced on the right hand controller
        /// </summary>
        internal AffordanceTooltipPlacementOverride[] leftPlacementOverrides { get { return m_LeftPlacementOverrides; } }
    }
}
#endif
