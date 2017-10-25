#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;
using UnityEngine.InputNew;

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

        [Serializable]
        public class AffordanceVisibilityDefinition
        {
            [SerializeField]
            VisibilityControlType m_VisibilityType;

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
            public VisibilityControlType visibilityType { get { return m_VisibilityType; } set { m_VisibilityType = value; } }
            public string colorProperty { get { return m_ColorProperty; } set { m_ColorProperty = value; } }
            public string alphaProperty { get { return m_AlphaProperty; } set { m_AlphaProperty = value; } }
        }

        [Serializable]
        public class AffordanceAnimationDefinition
        {
            [FlagsProperty]
            [SerializeField]
            AxisFlags m_TranslateAxes;

            [FlagsProperty]
            [SerializeField]
            AxisFlags m_RotateAxes;

            [SerializeField]
            float m_Min;

            [SerializeField]
            float m_Max = 5f;

            [SerializeField]
            bool m_ReverseForRightHand;

            public AxisFlags translateAxes { get { return m_TranslateAxes; } set { m_TranslateAxes = value; } }
            public AxisFlags rotateAxes { get { return m_RotateAxes; } set { m_RotateAxes = value; } }
            public float min { get { return m_Min; } set { m_Min = value; } }
            public float max { get { return m_Max; } set { m_Max = value; } }
            public bool reverseForRightHand { get { return m_ReverseForRightHand; } set { m_ReverseForRightHand = value; } }
        }

        [Serializable]
        public class AffordanceDefinition
        {
            [SerializeField]
            VRInputDevice.VRControl m_Control;

            [SerializeField]
            AffordanceAnimationDefinition m_AnimationDefinition;

            [SerializeField]
            AffordanceVisibilityDefinition m_VisibilityDefinition;

            public VRInputDevice.VRControl control { get { return m_Control; } set { m_Control = value; } }
            public AffordanceVisibilityDefinition visibilityDefinition { get { return m_VisibilityDefinition; } set { m_VisibilityDefinition = value; } }
            public AffordanceAnimationDefinition animationDefinition { get { return m_AnimationDefinition; } set { m_AnimationDefinition = value; } }
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
