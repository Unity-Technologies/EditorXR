using System;
using UnityEngine;

namespace Unity.Labs.EditorXR.Proxies
{
    class ViveProxyHelper : MonoBehaviour
    {
#pragma warning disable 649
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
#pragma warning restore 649

        /// <summary>
        /// Tooltip placement arrays to be replaced on the right hand controller
        /// </summary>
        internal AffordanceTooltipPlacementOverride[] leftPlacementOverrides { get { return m_LeftPlacementOverrides; } }
    }
}
