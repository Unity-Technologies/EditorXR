#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using VisibilityControlType = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.VisibilityControlType;
using AffordanceDefinition = UnityEditor.Experimental.EditorVR.Core.ProxyAffordanceMap.AffordanceDefinition;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    public class ProxyUI : MonoBehaviour
    {
        const float k_FadeInSpeedScalar = 4f;
        const float k_FadeOutSpeedScalar = 0.5f;
        const string k_ZWritePropertyName = "_ZWrite";

        List<Renderer> m_BodyRenderers; // Renderers not associated with affordances/controls, & will be HIDDEN when displaying feedback/tooltips
        List<Material> m_BodySwapOriginalMaterials; // Material collection used when swapping materials
        List<Renderer> m_AffordanceRenderers; // Renderers associated with affordances/controls, & will be SHOWN when displaying feedback/tooltips
        bool m_BodyRenderersVisible = true; // Body renderers default to visible/true
        bool m_AffordanceRenderersVisible = true; // Affordance renderers default to visible/true
        Affordance[] m_Affordances;
        Coroutine m_BodyVisibilityCoroutine;

        /// <summary>
        /// Bool that denotes the ProxyUI has been setup
        /// </summary>
        bool m_ProxyUISetup;

        /// <summary>
        /// Collection of proxy origins under which not to perform any affordance/body visibility changes
        /// </summary>
        List<Transform> m_ProxyOrigins;

        // Map of unique body materials to their original Colors (used for affordances with the "color" visibility control type)
        // The second param, ColorPair, houses the original cached color, and a value, representing the color to lerp FROM when animating visibility
        Dictionary<Material, affordancePropertyTuple<Color>> m_BodyMaterialOriginalColorMap = new Dictionary<Material, affordancePropertyTuple<Color>>();
        Dictionary<Material, affordancePropertyTuple<float>> m_BodyMaterialOriginalAlphaMap = new Dictionary<Material, affordancePropertyTuple<float>>();

        [SerializeField]
        ProxyAffordanceMap m_AffordanceMap;

        /// <summary>
        /// Model containing original value, and values to "animate from", unique to each body MeshRenderer material.
        /// Visibility of all objects in the proxy body are driven by a single AffordanceVisibilityDefinition,
        /// as opposed to individual interactable affordances, which each have their own AffordanceVisibilityDefinition, which contains their unique value data.
        /// This is a lightweight class to store that data, alleviating the need to duplicate an affordance definition for each body renderer as well.
        /// </summary>
        class affordancePropertyTuple<T>
        {
            public T originalValue { get; private set; }
            public T animateFromValue { get; set; }

            public affordancePropertyTuple(T originalValue, T animateFromValue)
            {
                this.originalValue = originalValue;
                this.animateFromValue = animateFromValue;
            }
        }
        /// <summary>
        /// Set the visibility of the affordance renderers that are associated with controls/input
        /// </summary>
        public bool affordancesVisible
        {
            set
            {
                if (!m_ProxyUISetup || m_ProxyOrigins == null || !gameObject.activeInHierarchy || m_AffordanceRenderersVisible == value)
                    return;

                m_AffordanceRenderersVisible = value;
                foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
                {
                    var visibilityDefinition = affordanceDefinition.visibilityDefinition;
                    switch (visibilityDefinition.visibilityType)
                    {
                        case VisibilityControlType.colorProperty:
                            this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceColorVisibility(value, affordanceDefinition));
                            break;
                        case VisibilityControlType.alphaProperty:
                            this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceAlphaVisibility(value, affordanceDefinition));
                            break;
                        case VisibilityControlType.materialSwap:
                            SwapAffordanceToHiddenMaterial(value, affordanceDefinition);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Set the visibility of the renderers not associated with controls/input
        /// </summary>
        public bool bodyVisible
        {
            set
            {
                if (!m_ProxyUISetup || m_ProxyOrigins == null || !gameObject.activeInHierarchy || m_BodyRenderersVisible == value)
                    return;

                m_BodyRenderersVisible = value;
                var visibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
                switch (visibilityDefinition.visibilityType)
                {
                    case VisibilityControlType.colorProperty:
                        this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateBodyColorVisibility(value));
                        break;
                    case VisibilityControlType.alphaProperty:
                        this.RestartCoroutine(ref m_BodyVisibilityCoroutine, AnimateBodyAlphaVisibility(value));
                        break;
                    case VisibilityControlType.materialSwap:
                        SwapBodyToHiddenMaterial(value);
                        break;
                }
            }
        }

        void OnDestroy()
        {
            // Cleanup cloned materials
            foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
            {
                var visibilityDefinition = affordanceDefinition.visibilityDefinition;
                var visibilityType = visibilityDefinition.visibilityType;
                if (visibilityType == VisibilityControlType.colorProperty || visibilityType == VisibilityControlType.alphaProperty)
                {
                    var materialsAndAssociatedColors = visibilityDefinition.materialsAndAssociatedColors;
                    if (materialsAndAssociatedColors == null)
                        continue;;

                    foreach (var materialToAssociatedColors in visibilityDefinition.materialsAndAssociatedColors)
                    {
                        var material = materialToAssociatedColors.firstElement;
                        if (material != null)
                            ObjectUtils.Destroy(material);
                    }
                }
            }

            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            var bodyVisibilityType = bodyVisibilityDefinition.visibilityType;
            if (bodyVisibilityType == VisibilityControlType.colorProperty || bodyVisibilityType == VisibilityControlType.alphaProperty)
            {
                foreach (var kvp in m_BodyMaterialOriginalColorMap)
                {
                    var material = kvp.Key;
                    if (material != null)
                        ObjectUtils.Destroy(material);
                }
            }
            else if (bodyVisibilityType == VisibilityControlType.materialSwap)
            {
                foreach (var material in m_BodySwapOriginalMaterials)
                {
                    if (material != null)
                        ObjectUtils.Destroy(material);
                }
            }
        }

        /// <summary>
        /// Setup this ProxyUI
        /// </summary>
        /// <param name="affordances">The ProxyHelper affordances that drive visual changes in the ProxyUI</param>
        /// <param name="proxyOrigins">ProxyOrigins whose child renderers will not be controlled by the PRoxyUI</param>
        public void Setup(Affordance[] affordances, List<Transform> proxyOrigins)
        {
            // Prevent multiple setups
            if (m_ProxyUISetup)
            {
                Debug.LogError("ProxyUI can only be setup once on : " + gameObject.name);
                return;
            }

            // Don't allow setup if affordances are already set
            if (m_Affordances != null)
            {
                Debug.LogError("Affordances are already set on : " + gameObject.name);
                return;
            }

            // Don't allow setup if affordances are invalid
            if (affordances == null || affordances.Length == 0)
            {
                Debug.LogError("Affordances invalid when attempting to setup ProxyUI on : " + gameObject.name);
                return;
            }

            // Prevent further setup if affordance map isn't assigned
            if (m_AffordanceMap == null)
            {
                Debug.LogError("An Affordance Map must be assigned to ProxyUI on : " + gameObject.name);
                return;
            }

            // Set affordances AFTER setting origins, as the origins are referenced when setting up affordances
            m_ProxyOrigins = proxyOrigins;

            // Clone the affordance map, in order to allow a single map to drive the visuals of many duplicate
            // This also allows coroutine sets in the ProxyAffordanceMap to be performed simultaneously for n-number of devices in a proxy
            m_AffordanceMap = Instantiate(m_AffordanceMap);

            m_Affordances = affordances;
            m_AffordanceRenderers = new List<Renderer>();
            foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
            {
                var control = affordanceDefinition.control;
                var affordance = m_Affordances.FirstOrDefault(x => x.control == control);
                if (affordance != null)
                {
                    // Setup animated transparency for all materials associated with all renderers under the control's transform
                    var renderers = affordance.renderer.GetComponentsInChildren<Renderer>(true);
                    if (renderers != null)
                    {
                        var visibilityDefinition = affordanceDefinition.visibilityDefinition;;
                        foreach (var renderer in renderers)
                        {
                            var materialClones = MaterialUtils.CloneMaterials(renderer); // Clone all materials associated with the renderer
                            if (materialClones != null)
                            {
                                // Add to collection for later optimized comparison against body renderers
                                // Also stay in sync with visibilityDefinition.materialsAndAssociatedColors collection
                                m_AffordanceRenderers.Add(renderer);

                                var visibilityType = visibilityDefinition.visibilityType;
                                var hiddenColor = visibilityDefinition.hiddenColor;
                                var hiddenAlphaEncodedInColor = visibilityDefinition.hiddenAlpha * Color.white;
                                var shaderAlphaPropety = visibilityDefinition.alphaProperty;
                                var materialsAndAssociatedColors = new List<Tuple<Material, Color, Color, Color, Color>>();
                                // Material, original color, hidden color, animateFromColor(used by animating coroutines, not initialized here)
                                foreach (var material in materialClones)
                                {
                                    // Clones that utilize the standard shader can be cloned and lose their enabled ZWrite value (1), if it was enabled on the material
                                    // Set it again, to avoid ZWrite + transparency visual issues
                                    if (visibilityType != VisibilityControlType.materialSwap && material.HasProperty(k_ZWritePropertyName))
                                        material.SetFloat(k_ZWritePropertyName, 1);

                                    Tuple<Material, Color, Color, Color, Color> materialAndAssociatedColors = null;
                                    switch (visibilityDefinition.visibilityType)
                                    {
                                        case VisibilityControlType.colorProperty:
                                            var originalColor = material.GetColor(visibilityDefinition.colorProperty);
                                            materialAndAssociatedColors = new Tuple<Material, Color, Color, Color, Color>(material, originalColor, hiddenColor, Color.clear, Color.clear);
                                            break;
                                        case VisibilityControlType.alphaProperty:
                                            var originalAlpha = material.GetFloat(shaderAlphaPropety);
                                            var originalAlphaEncodedInColor = Color.white * originalAlpha;
                                            // When animating based on alpha, use the Color.a value of the original, hidden, and animateFrom colors set below
                                            materialAndAssociatedColors = new Tuple<Material, Color, Color, Color, Color>(material, originalAlphaEncodedInColor, hiddenAlphaEncodedInColor, Color.clear, Color.clear);
                                            break;
                                    }

                                    materialsAndAssociatedColors.Add(materialAndAssociatedColors);
                                }

                                visibilityDefinition.materialsAndAssociatedColors = materialsAndAssociatedColors;
                            }
                        }
                    }
                }
            }

            // Collect renderers not associated with affordances
            // Material swaps don't need to cache original values, only alpha & color
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            m_BodyRenderers = GetComponentsInChildren<Renderer>(true).Where(x => !m_AffordanceRenderers.Contains(x) && !IsChildOfProxyOrigin(x.transform)).ToList();
            switch (bodyVisibilityDefinition.visibilityType)
            {
                case VisibilityControlType.colorProperty:
                    foreach (var renderer in m_BodyRenderers)
                    {
                        // TODO: support for skipping the cloning of materials in the body that are shared between objects, in order to reduce draw calls
                        var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
                        if (materialClone != null)
                        {
                            var originalColor = materialClone.GetColor(bodyVisibilityDefinition.colorProperty);
                            if (materialClone.HasProperty(k_ZWritePropertyName))
                                materialClone.SetFloat(k_ZWritePropertyName, 1);

                            m_BodyMaterialOriginalColorMap[materialClone] = new affordancePropertyTuple<Color>(originalColor, originalColor);
                        }
                    }
                    break;
                case VisibilityControlType.alphaProperty:
                    string shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
                    foreach (var renderer in m_BodyRenderers)
                    {
                        var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
                        if (materialClone != null)
                        {
                            var originalAlpha = materialClone.GetFloat(shaderAlphaPropety);
                            if (materialClone.HasProperty(k_ZWritePropertyName))
                                materialClone.SetFloat(k_ZWritePropertyName, 1);

                            m_BodyMaterialOriginalAlphaMap[materialClone] = new affordancePropertyTuple<float>(originalAlpha, originalAlpha);
                        }
                    }
                    break;
                case VisibilityControlType.materialSwap:
                    m_BodySwapOriginalMaterials = new List<Material>();
                    foreach (var renderer in m_BodyRenderers)
                    {
                        var materialClone = MaterialUtils.GetMaterialClone(renderer); // TODO: support multiple materials per-renderer
                        m_BodySwapOriginalMaterials.Add(materialClone);
                    }
                    break;
            }

            affordancesVisible = false;
            bodyVisible = false;

            // Allow setting of affordance & body visibility after affordance+body setup is performed in the "affordances" property
            m_ProxyUISetup = true;
        }

        IEnumerator AnimateAffordanceColorVisibility(bool isVisible, AffordanceDefinition definition)
        {
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var visibilityDefinition = definition.visibilityDefinition;
            var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            var shaderColorPropety = visibilityDefinition.colorProperty;

            if (materialsAndColors == null)
                yield break;

            // Setup animateFromColors using the current color values of each material associated with all renderers drawing this affordance
            foreach (var materialAndAssociatedColors in materialsAndColors)
            {
                    var animateFromColor = materialAndAssociatedColors.firstElement.GetColor(shaderColorPropety); // Get current color from material
                    var animateToColor = isVisible ? materialAndAssociatedColors.secondElement : materialAndAssociatedColors.thirdElement; // (second)original or (third)hidden color(alpha/color.a)
                    materialAndAssociatedColors.fourthElement = animateFromColor;
                    materialAndAssociatedColors.fifthElement = animateToColor;
            }

            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar);
                foreach (var materialAndAssociatedColors in materialsAndColors)
                {
                    var currentColor = Color.Lerp(materialAndAssociatedColors.fourthElement, materialAndAssociatedColors.fifthElement, smoothedAmount);
                    materialAndAssociatedColors.firstElement.SetColor(shaderColorPropety, currentColor);
                }

                yield return null;
            }
        }

        IEnumerator AnimateAffordanceAlphaVisibility(bool isVisible, AffordanceDefinition definition)
        {
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var visibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            var shaderAlphaPropety = visibilityDefinition.alphaProperty;

            // Setup animateFromColors using the current color values of each material associated with all renderers drawing this affordance
            foreach (var materialAndAssociatedColors in materialsAndColors)
            {
                var animateFromAlpha = materialAndAssociatedColors.firstElement.GetFloat(shaderAlphaPropety); // Get current alpha from material
                var animateToAlpha = isVisible ? materialAndAssociatedColors.secondElement.a : materialAndAssociatedColors.thirdElement.a; // (second)original or (third)hidden color(alpha/color.a)
                materialAndAssociatedColors.fourthElement = Color.white * animateFromAlpha; // Encode the alpha for the FROM color value, color.a
                materialAndAssociatedColors.fifthElement = Color.white * animateToAlpha; // // Encode the alpha for the TO color value, color.a
            }

            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar);
                foreach (var materialAndAssociatedColors in materialsAndColors)
                {
                    var currentAlpha = Color.Lerp(materialAndAssociatedColors.fourthElement, materialAndAssociatedColors.fifthElement, smoothedAmount);
                    materialAndAssociatedColors.firstElement.SetFloat(shaderAlphaPropety, currentAlpha.a); // Alpha is encoded in color.a
                }

                yield return null;
            }
        }

        IEnumerator AnimateBodyColorVisibility(bool isVisible)
        {
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            foreach (var kvp in m_BodyMaterialOriginalColorMap)
            {
                kvp.Value.animateFromValue = kvp.Key.GetColor(bodyVisibilityDefinition.colorProperty);
            }

            const float kTargetAmount = 1f;
            const float kHiddenValue = 0.25f;
            var speedScalar = isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var shaderColorPropety = bodyVisibilityDefinition.colorProperty;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar);
                foreach (var kvp in m_BodyMaterialOriginalColorMap)
                {
                    // Set original cached color when visible, transparent when hidden
                    var valueFrom = kvp.Value.animateFromValue;
                    var valueTo = isVisible ? kvp.Value.originalValue : new Color(valueFrom.r, valueFrom.g, valueFrom.b, kHiddenValue);
                    var currentColor = Color.Lerp(valueFrom, valueTo, smoothedAmount);
                    kvp.Key.SetColor(shaderColorPropety, currentColor);
                }

                yield return null;
            }

            // Mandate target values have been set
            foreach (var kvp in m_BodyMaterialOriginalColorMap)
            {
                var originalColor = kvp.Value.originalValue;
                kvp.Key.SetColor(shaderColorPropety, isVisible ? originalColor : new Color(originalColor.r, originalColor.g, originalColor.b, kHiddenValue));
            }
        }

        IEnumerator AnimateBodyAlphaVisibility(bool isVisible)
        {
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
            {
                kvp.Value.animateFromValue = kvp.Key.GetFloat(bodyVisibilityDefinition.alphaProperty);
            }

            const float kTargetAmount = 1f;
            const float kHiddenValue = 0.25f;
            var speedScalar = isVisible ? k_FadeInSpeedScalar : k_FadeOutSpeedScalar;
            var shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
            var currentAmount = 0f;
            while (currentAmount < kTargetAmount)
            {
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount += Time.unscaledDeltaTime * speedScalar);
                foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
                {
                    var valueFrom = kvp.Value.animateFromValue;
                    var valueTo = isVisible ? kvp.Value.originalValue : kHiddenValue;
                    var currentAlpha = Mathf.Lerp(valueFrom, valueTo, smoothedAmount);
                    kvp.Key.SetFloat(shaderAlphaPropety, currentAlpha);
                }

                yield return null;
            }

            // Mandate target values have been set
            foreach (var kvp in m_BodyMaterialOriginalAlphaMap)
            {
                kvp.Key.SetFloat(shaderAlphaPropety, isVisible ? kvp.Value.originalValue : kHiddenValue);
            }
        }

        void SwapAffordanceToHiddenMaterial(bool swapToHiddenMaterial, AffordanceDefinition definition)
        {
            var visibilityDefinition = definition.visibilityDefinition;
            var materialsAndColors = visibilityDefinition.materialsAndAssociatedColors;
            for (var i = 0; i < materialsAndColors.Count; ++i)
            {
                var swapMaterial = swapToHiddenMaterial ? visibilityDefinition.hiddenMaterial : materialsAndColors[i].firstElement;
                // m_AffordanceRenderers is created/added in sync with the order of the materialsAndAssociatedColors in the affordance visibility definition
                m_AffordanceRenderers[i].material = swapMaterial; // Set swapped material in associated renderer
            }
        }

        void SwapBodyToHiddenMaterial(bool swapToHiddenMaterial)
        {
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            for (var i = 0; i < m_BodyRenderers.Count; ++i)
            {
                var renderer = m_BodyRenderers[i];
                var swapMaterial = swapToHiddenMaterial ? bodyVisibilityDefinition.hiddenMaterial : m_BodySwapOriginalMaterials[i];
                renderer.material = swapMaterial;
            }
        }

        bool IsChildOfProxyOrigin(Transform transform)
        {
            // Prevent any menu/origin content from having its visibility changed
            var isChild = false;
            foreach (var origin in m_ProxyOrigins)
            {
                // m_ProxyOrgins is populated by ProxyHelper
                // Validate that the transform param is not a child of any proxy origins
                // Those objects shouldn't have their visibility changed by the ProxyUI
                // Their individual controllers should handle their visibility
                if (transform.IsChildOf(origin))
                {
                    isChild = true;
                    break;
                }
            }

            return isChild;
        }
    }
}
#endif
