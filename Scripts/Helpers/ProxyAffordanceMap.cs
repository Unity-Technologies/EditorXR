#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorVR/EXR Proxy Affordance Map", fileName = "NewProxyAffordanceMap.asset")]
    public class ProxyAffordanceMap : ScriptableObject
    {
        public enum VisibilityControlType
        {
            colorProperty,
            alphaProperty,
            materialSwap
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

        public AffordanceDefinition[] AffordanceDefinitions { get { return m_AffordanceDefinitions; } set { m_AffordanceDefinitions = value; } }
        public AffordanceVisibilityDefinition bodyVisibilityDefinition { get { return m_BodyVisibilityDefinition; } }
        public AffordanceVisibilityDefinition defaultAffordanceVisibilityDefinition { get { return m_DefaultAffordanceVisibilityDefinition; } }
        public AffordanceAnimationDefinition defaultAnimationDefinition { get { return m_DefaultAffordanceAnimationDefinition; } set { m_DefaultAffordanceAnimationDefinition = value; } }
    }
}
#endif
