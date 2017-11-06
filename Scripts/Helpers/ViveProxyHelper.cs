using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    public class ViveProxyHelper : MonoBehaviour
    {
        [Serializable]
        public class AffordanceTooltipPlacementOverride
        {
            public AffordanceTooltip tooltip;
            public AffordanceTooltipPlacement[] placements;
        }

        [FormerlySerializedAs("m_RightPlacementOverrides")]
        [SerializeField]
        AffordanceTooltipPlacementOverride[] m_LeftPlacementOverrides;

        /// <summary>
        /// Tooltip placement arrays to be replaced on the right hand controller
        /// </summary>
        internal AffordanceTooltipPlacementOverride[] leftPlacementOverrides { get { return m_LeftPlacementOverrides; } }
    }
}
