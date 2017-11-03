#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    /// <summary>
    /// Definition containing data utilized to change the visual appearance of an affordance for various actions
    /// </summary>
    [Serializable]
    public class AffordanceVisibilityDefinition
    {
        public class AffordanceVisualStateData
        {
            /// <summary>
            /// The renderer for this visual state
            /// </summary>
            public Renderer renderer { get; set; }

            /// <summary>
            /// The material for this visual state
            /// </summary>
            public Material material { get; set; }

            /// <summary>
            /// Original color, Color.a is used for alpha-only animation
            /// </summary>
            public Color originalColor { get; set; }

            /// <summary>
            /// Hidden color, Color.a is used for alpha-only animation
            /// </summary>
            public Color hiddenColor { get; set; }

            /// <summary>
            /// Current color of the material at runtime
            /// </summary>
            public Color currentColor { get; set; }
        }

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

        readonly List<AffordanceVisualStateData> m_VisualStateData = new List<AffordanceVisualStateData>();

        /// <summary>
        /// Data defining the original, and current visual state of an affordance
        /// </summary>
        public List<AffordanceVisualStateData> visualStateData { get { return m_VisualStateData; } }

        /// <summary>
        /// The hidden color of the material
        /// </summary>
        public Color hiddenColor
        {
            get{ return m_HiddenColor; }
            set { m_HiddenColor = value; }
        }

        /// <summary>
        /// The hidden alpha value of the material
        /// </summary>
        public float hiddenAlpha
        {
            get { return m_HiddenAlpha; }
            set { m_HiddenAlpha = value; }
        }

        /// <summary>
        /// The material to with which to swap instead of animating visibility (material blending is not supported)
        /// </summary>
        public Material hiddenMaterial
        {
            get { return m_HiddenMaterial; }
            set { m_HiddenMaterial = value; }
        }

        /// <summary>
        /// The coroutine that performs animated visibility changes for this affordance
        /// </summary>
        public Coroutine affordanceVisibilityCoroutine;

        /// <summary>
        /// The type of visibility changes that will be performed on this affordance (color change, alpha change, material swap, etc)
        /// </summary>
        public ProxyAffordanceMap.VisibilityControlType visibilityType
        {
            get { return m_VisibilityType; }
            set { m_VisibilityType = value; }
        }

        /// <summary>
        /// The name of the shader parameter utilized to animate color changes to this affordance, if the visibility type is set to COLOR
        /// </summary>
        public string colorProperty
        {
            get { return m_ColorProperty; }
            set { m_ColorProperty = value; }
        }

        /// <summary>
        /// The name of the shader parameter utilized to animate alpha changes to this affordance, if the visibility type is set to ALPHA
        /// </summary>
        public string alphaProperty
        {
            get { return m_AlphaProperty; }
            set { m_AlphaProperty = value; }
        }

        /// <summary>
        /// Whether this affordance is visible (at runtime)
        /// </summary>
        public bool visible { get; set; }

        // HACK: if empty constructor is missing, m_VisualStateData is not initialized
        AffordanceVisibilityDefinition()
        {
        }

        public AffordanceVisibilityDefinition(AffordanceVisibilityDefinition definitionToCopy)
        {
            Debug.Log(m_VisualStateData);
            visibilityType = definitionToCopy.visibilityType;
            colorProperty = definitionToCopy.colorProperty;
            alphaProperty = definitionToCopy.alphaProperty;
            hiddenColor = definitionToCopy.hiddenColor;
            hiddenAlpha = definitionToCopy.hiddenAlpha;
            hiddenMaterial = definitionToCopy.hiddenMaterial;
        }
    }
}
#endif
