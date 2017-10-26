#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [Serializable]
    public class AffordanceVisibilityDefinition
    {
        [SerializeField]
        ProxyAffordanceMap.VisibilityControlType m_VisibilityType;

        [SerializeField] // colorProperty field
        string m_ColorProperty = "_Color"; // Consider custom inspector that only displays this if this visibility type is chosen

        [SerializeField] // alphaProperty field
        string m_AlphaProperty = "_Alpha"; // Consider custom inspector that only displays this if this visibility type is chosen

        [SerializeField]
        Color m_HiddenColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField]
        float m_HiddenAlpha;

        [SerializeField]
        Material m_HiddenMaterial;

        public class AffordanceVisualStateData
        {
            /// <summary>
            /// Original material
            /// </summary>
            public Material originalMaterial { get; set; }

            /// <summary>
            /// Original color, Color.a is used for alpha-only animation
            /// </summary>
            public Color originalColor { get; set; }

            /// <summary>
            /// Hidden color, Color.a is used for alpha-only animation
            /// </summary>
            public Color hiddenColor { get; set; }

            /// <summary>
            /// Animate FROM color (used in animated coroutines), color.a is used for alpha-only animation
            /// </summary>
            public Color animateFromColor { get; set; }

            /// <summary>
            /// Animate TO color (used in animated coroutines), color.a is used for alpha-only animation
            /// </summary>
            public Color animateToColor { get; set; }

            public AffordanceVisualStateData(Material originalMaterial, Color originalColor, Color hiddenColor, Color animateFromColor, Color animateToColor)
            {
                this.originalMaterial = originalMaterial;
                this.originalColor = originalColor;
                this.hiddenColor = hiddenColor;
                this.animateFromColor = animateFromColor;
                this.animateToColor = animateToColor;
            }
        }

        /// <summary>
        /// Data defining the original, and current visual state of an affordance
        /// </summary>
        public List<AffordanceVisualStateData> visualStateData { get; set; }

        /// <summary>
        /// The hidden color of the material
        /// </summary>
        public Color hiddenColor { get { return m_HiddenColor; } set { m_HiddenColor = value; } }

        /// <summary>
        /// The hidden alpha value of the material
        /// </summary>
        public float hiddenAlpha { get { return m_HiddenAlpha; } set { m_HiddenAlpha = value; } }

        /// <summary>
        /// The material to with which to swap instead of animating visibility (material blending is not supported)
        /// </summary>
        public Material hiddenMaterial { get { return m_HiddenMaterial; } set { m_HiddenMaterial = value; } }

        public Coroutine affordanceVisibilityCoroutine;
        public ProxyAffordanceMap.VisibilityControlType visibilityType { get { return m_VisibilityType; } set { m_VisibilityType = value; } }
        public string colorProperty { get { return m_ColorProperty; } set { m_ColorProperty = value; } }
        public string alphaProperty { get { return m_AlphaProperty; } set { m_AlphaProperty = value; } }
    }
}
#endif
