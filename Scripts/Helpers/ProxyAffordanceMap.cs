#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CreateAssetMenu(menuName = "EditorVR/EXR Proxy Affordance Map", fileName = "NewProxyAffordanceMap.asset")]
    public class ProxyAffordanceMap : ScriptableObject
    {
        // TODO REMOVE - 5.6 HACK that remedies items not appearing in the create menu
#if UNITY_EDITOR
        [MenuItem("Assets/Create/EditorVR/EditorVR Proxy Affordance Map")]
        public static void Create()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
                path = "Assets";

            if (!Directory.Exists(path))
                path = Path.GetDirectoryName(path);

            var affordanceMap = ScriptableObject.CreateInstance<ProxyAffordanceMap>();
            path = AssetDatabase.GenerateUniqueAssetPath(path + "/NewProxyAffordanceMap.asset");
            AssetDatabase.CreateAsset(affordanceMap, path);
        }
#endif

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

            //TODO: Add custom Inspector : Expose each visibility control type field set based on the selected visibility control type
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

            /// <summary>
            /// The renderer & cloned materials associated with this affordance
            /// Cloned materials associated with the renderer will have their properties animated by the ProxyUI
            /// Element 1: Original material
            /// Element 2: Original color, Color.a is used for alpha-only animation
            /// Element 3: Hidden color, Color.a is used for alpha-only animation
            /// Element 4: Animate FROM color (used in animated coroutines), color.a is used for alpha-only animation
            /// Element 5: Animate TO color (used in animated coroutines), color.a is used for alpha-only animation
            /// </summary>
            public List<Tuple<Material, Color, Color, Color, Color>> materialsAndAssociatedColors { get; set; }

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
        public class AffordanceDefinition
        {
            [SerializeField]
            VRInputDevice.VRControl m_Control;

            [SerializeField]
            AffordanceVisibilityDefinition m_VisibilityDefinition;

            public VRInputDevice.VRControl control { get { return m_Control; } set { m_Control = value; } }
            public AffordanceVisibilityDefinition visibilityDefinition { get { return m_VisibilityDefinition; } set { m_VisibilityDefinition = value; } }
        }

        [Header("Non-Interactive Input-Device Body Elements")]
        [SerializeField]
        AffordanceVisibilityDefinition m_BodyVisibilityDefinition;

        [Space(20)]
        [Header("Affordances / Interactive Input-Device Elements")]
        [SerializeField]
        AffordanceVisibilityDefinition m_DefaultAffordanceVisibilityDefinition;

        [Header("Custom Affordance Overrides")]
        [SerializeField]
        AffordanceDefinition[] m_AffordanceDefinitions;

        public AffordanceDefinition[] AffordanceDefinitions { get { return m_AffordanceDefinitions; } set { m_AffordanceDefinitions = value; } }
        public AffordanceVisibilityDefinition bodyVisibilityDefinition { get { return m_BodyVisibilityDefinition; } }
        public AffordanceVisibilityDefinition defaultAffordanceVisibilityDefinition { get { return m_DefaultAffordanceVisibilityDefinition; } }
    }
}
#endif
