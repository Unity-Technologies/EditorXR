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
            public AffordanceTooltip tooltip;
            public AffordanceTooltipPlacement[] placements;
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
