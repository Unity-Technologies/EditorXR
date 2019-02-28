using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Definition containing data utilized to change the visual appearance of an affordance for various actions
    /// </summary>
    [Serializable]
    public class AffordanceVisibilityDefinition
    {
#pragma warning disable 649
        [SerializeField]
        ProxyAffordanceMap.VisibilityControlType m_VisibilityType;

        [SerializeField] // shader color property name field
        string m_ColorProperty = "_Color";

        [SerializeField] // shader alpha property name field
        string m_AlphaProperty = "_Alpha";

        [SerializeField]
        Color m_HiddenColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField]
        float m_HiddenAlpha;

        [SerializeField]
        Material m_HiddenMaterial;
#pragma warning restore 649

        /// <summary>
        /// The hidden color of the material
        /// </summary>
        public Color hiddenColor { get{ return m_HiddenColor; } }

        /// <summary>
        /// The hidden alpha value of the material
        /// </summary>
        public float hiddenAlpha { get { return m_HiddenAlpha; } }

        /// <summary>
        /// The material to with which to swap instead of animating visibility (material blending is not supported)
        /// </summary>
        public Material hiddenMaterial { get { return m_HiddenMaterial; } }

        /// <summary>
        /// The coroutine that performs animated visibility changes for this affordance
        /// </summary>
        public Coroutine affordanceVisibilityCoroutine;

        /// <summary>
        /// The type of visibility changes that will be performed on this affordance (color change, alpha change, material swap, etc)
        /// </summary>
        public ProxyAffordanceMap.VisibilityControlType visibilityType { get { return m_VisibilityType; } }

        /// <summary>
        /// The name of the shader parameter utilized to animate color changes to this affordance, if the visibility type is set to COLOR
        /// </summary>
        public string colorProperty { get { return m_ColorProperty; } }

        /// <summary>
        /// The name of the shader parameter utilized to animate alpha changes to this affordance, if the visibility type is set to ALPHA
        /// </summary>
        public string alphaProperty { get { return m_AlphaProperty; } }
    }
}
