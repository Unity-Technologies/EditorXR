
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorXR/Proxy Affordance Map", fileName = "NewProxyAffordanceMap.asset")]
    public class ProxyAffordanceMap : ScriptableObject
    {
        /// <summary>
        /// The type of visibility changes that will be performed on this affordance (color change, alpha change, material swap, etc)
        /// </summary>
        public enum VisibilityControlType
        {
            ColorProperty,
            AlphaProperty,
            MaterialSwap
        }

        [Header("Non-Interactive Input-Device Body Elements")]
        [SerializeField]
        AffordanceVisibilityDefinition m_BodyVisibilityDefinition;

        [Header("Affordances / Interactive Input-Device Elements")]
        [SerializeField]
        AffordanceAnimationDefinition m_DefaultAffordanceAnimationDefinition;

        [SerializeField]
        AffordanceVisibilityDefinition m_DefaultAffordanceVisibilityDefinition;

        [Header("Custom Affordance Overrides")]
        [SerializeField]
        AffordanceDefinition[] m_AffordanceDefinitions;

        /// <summary>
        /// Collection of affordance definitions representing a proxy
        /// </summary>
        public AffordanceDefinition[] AffordanceDefinitions { get { return m_AffordanceDefinitions; } }

        /// <summary>
        /// Default visibility definition/data used to drive the visual changes of (non-affordance) body visual elements
        /// </summary>
        public AffordanceVisibilityDefinition bodyVisibilityDefinition { get { return m_BodyVisibilityDefinition; } }

        /// <summary>
        /// Default visibility definition/data used to drive the visual changes of affordances lacking a custom/override visibility definition
        /// </summary>
        public AffordanceVisibilityDefinition defaultAffordanceVisibilityDefinition { get { return m_DefaultAffordanceVisibilityDefinition; } }

        /// <summary>
        /// Default animation definition/data used to drive the translation/rotation of affordances lacking a custom/override animation definition
        /// </summary>
        public AffordanceAnimationDefinition defaultAnimationDefinition { get { return m_DefaultAffordanceAnimationDefinition; } }
    }
}

