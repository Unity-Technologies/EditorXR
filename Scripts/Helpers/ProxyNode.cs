#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    using AffordanceVisualStateData = AffordanceVisibilityDefinition.AffordanceVisualStateData;
    using FeedbackRequestTuple = Tuple<ProxyFeedbackRequest, Coroutine>;
    using VisibilityControlType = ProxyAffordanceMap.VisibilityControlType;

    /// <summary>
    /// ProxyFeedbackRequests reside in feedbackRequest collection until the action associated with an affordance changes
    /// Some are removed immediately after being added; others exist for the duration of an action/tool's lifespan
    /// </summary>
    public class ProxyFeedbackRequest : FeedbackRequest
    {
        public int priority;
        public VRInputDevice.VRControl control;
        public Node node;
        public string tooltipText;
        public bool suppressExisting;
        public bool visible;
        public bool proxyShaken;
    }

    class ProxyNode : MonoBehaviour, ISetTooltipVisibility, ISetHighlight, IConnectInterfaces
    {
        const string k_ZWritePropertyName = "_ZWrite";
        const float k_DefaultFeedbackDuration = 5f;

        /// <summary>
        /// Model containing original value, and values to "animate from", unique to each body MeshRenderer material.
        /// Visibility of all objects in the proxy body are driven by a single AffordanceVisibilityDefinition,
        /// as opposed to individual interactable affordances, which each have their own AffordanceVisibilityDefinition, which contains their unique value data.
        /// This is a lightweight class to store that data, alleviating the need to duplicate an affordance definition for each body renderer as well.
        /// </summary>
        class AffordancePropertyTuple<T>
        {
            public T originalValue { get; private set; }
            public T animateFromValue { get; set; }

            public AffordancePropertyTuple(T originalValue, T animateFromValue)
            {
                this.originalValue = originalValue;
                this.animateFromValue = animateFromValue;
            }
        }

        [SerializeField]
        float m_FadeInSpeedScalar = 4f;

        [SerializeField]
        float m_FadeOutSpeedScalar = 0.5f;

        [SerializeField]
        Transform m_RayOrigin;

        [SerializeField]
        Transform m_MenuOrigin;

        [SerializeField]
        Transform m_AlternateMenuOrigin;

        [SerializeField]
        Transform m_PreviewOrigin;

        [SerializeField]
        Transform m_FieldGrabOrigin;

        [SerializeField]
        ProxyAnimator m_ProxyAnimator;

        [SerializeField]
        ProxyAffordanceMap m_AffordanceMap;

        [Tooltip("Affordance objects that store transform, renderer, and tooltip references")]
        [SerializeField]
        Affordance[] m_Affordances;

        // Renderers associated with affordances/controls, & will be SHOWN when displaying feedback/tooltips
        readonly List<Renderer> m_AffordanceRenderers = new List<Renderer>();
        List<Renderer> m_BodyRenderers; // Renderers not associated with affordances/controls, & will be HIDDEN when displaying feedback/tooltips
        List<Material> m_BodySwapOriginalMaterials; // Material collection used when swapping materials
        bool m_BodyRenderersVisible = true; // Body renderers default to visible/true
        bool m_AffordanceRenderersVisible = true; // Affordance renderers default to visible/true
        Coroutine m_BodyVisibilityCoroutine;

        ProxyFeedbackRequest m_SemitransparentLockRequest;
        ProxyFeedbackRequest m_ShakeFeedbackRequest;

        FacingDirection m_FacingDirection;

        // Map of unique body materials to their original Colors (used for affordances with the "color" visibility control type)
        // The second param, ColorPair, houses the original cached color, and a value, representing the color to lerp FROM when animating visibility
        readonly Dictionary<Material, AffordancePropertyTuple<Color>> m_BodyMaterialOriginalColorMap = new Dictionary<Material, AffordancePropertyTuple<Color>>();
        readonly Dictionary<Material, AffordancePropertyTuple<float>> m_BodyMaterialOriginalAlphaMap = new Dictionary<Material, AffordancePropertyTuple<float>>();

        readonly List<FeedbackRequestTuple> m_FeedbackRequests = new List<FeedbackRequestTuple>();

        // Local method use only -- created here to reduce garbage collection
        static readonly List<FeedbackRequestTuple> k_FeedbackRequestsCopy = new List<FeedbackRequestTuple>();

        /// <summary>
        /// The transform that the device's ray contents (default ray, custom ray, etc) will be parented under
        /// </summary>
        public Transform rayOrigin { get { return m_RayOrigin; } }

        /// <summary>
        /// The transform that the menu content will be parented under
        /// </summary>
        public Transform menuOrigin { get { return m_MenuOrigin; } }

        /// <summary>
        /// The transform that the alternate-menu content will be parented under
        /// </summary>
        public Transform alternateMenuOrigin { get { return m_AlternateMenuOrigin; } }

        /// <summary>
        /// The transform that the display/preview objects will be parented under
        /// </summary>
        public Transform previewOrigin { get { return m_PreviewOrigin; } }

        /// <summary>
        /// The transform that the display/preview objects will be parented under
        /// </summary>
        public Transform fieldGrabOrigin { get { return m_FieldGrabOrigin; } }

        /// <summary>
        /// Set the visibility of the affordance renderers that are associated with controls/input
        /// </summary>
        public bool affordancesVisible
        {
            set
            {
                if (!gameObject.activeInHierarchy || m_AffordanceRenderersVisible == value)
                    return;

                m_AffordanceRenderersVisible = value;
                foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
                {
                    var visibilityDefinition = affordanceDefinition.visibilityDefinition;
                    switch (visibilityDefinition.visibilityType)
                    {
                        case VisibilityControlType.colorProperty:
                            this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceColorVisibility(value, affordanceDefinition, m_FadeInSpeedScalar, m_FadeOutSpeedScalar));
                            break;
                        case VisibilityControlType.alphaProperty:
                            this.RestartCoroutine(ref visibilityDefinition.affordanceVisibilityCoroutine, AnimateAffordanceAlphaVisibility(value, m_FadeInSpeedScalar, m_FadeOutSpeedScalar, m_AffordanceMap.bodyVisibilityDefinition));
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
                if (!gameObject.activeInHierarchy || m_BodyRenderersVisible == value)
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

        void Awake()
        {
            m_ShakeFeedbackRequest = new ProxyFeedbackRequest
            {
                control = VRInputDevice.VRControl.LocalPosition,
                node = Node.None,
                tooltipText = null,
                suppressExisting = true,
                proxyShaken = true
            };

            // Don't allow setup if affordances are invalid
            if (m_Affordances == null || m_Affordances.Length == 0)
            {
                Debug.LogError("Affordances invalid when attempting to setup ProxyUI on : " + gameObject.name);
                return;
            }

            // Prevent further setup if affordance map isn't assigned
            if (m_AffordanceMap == null)
            {
                Debug.LogError("A valid Affordance Map must be present when setting up ProxyUI on : " + gameObject.name);
                return;
            }

            // Clone the affordance map, in order to allow a single map to drive the visuals of many duplicate
            // This also allows coroutine sets in the ProxyAffordanceMap to be performed simultaneously for n-number of devices in a proxy
            m_AffordanceMap = Instantiate(m_AffordanceMap);

            // If no custom affordance definitions are defined in the affordance map, they will be populated by new generated definitions below
            var affordanceMapDefinitions = m_AffordanceMap.AffordanceDefinitions;
            var affordancesDefinedInMap = affordanceMapDefinitions != null && affordanceMapDefinitions.Length > 0 && affordanceMapDefinitions[0] != null;

            // If affordanceMapDefinitions is null, set the list below into the map after setup
            var generatedAffordanceDefinitions = new List<AffordanceDefinition>();
            var defaultAffordanceVisibilityDefinition = m_AffordanceMap.defaultAffordanceVisibilityDefinition;
            var defaultAffordanceAnimationDefinition = m_AffordanceMap.defaultAnimationDefinition;
            foreach (var proxyAffordance in m_Affordances)
            {
                var renderers = proxyAffordance.renderer.GetComponentsInChildren<Renderer>(true);
                if (renderers != null)
                {
                    // Setup animated color or alpha transparency for all materials associated with all renderers associated with the control
                    AffordanceVisibilityDefinition visibilityDefinition;
                    var control = proxyAffordance.control;

                    // Assemble a new affordance definition and visibility definition for the affordance,
                    // if a custom definition for the control was not defined in the AffordanceMap
                    var matchingAffordanceDefinition = affordancesDefinedInMap ? affordanceMapDefinitions.FirstOrDefault(x => x.control == control) : null;
                    if (matchingAffordanceDefinition == null)
                    {
                        // Deep copy the default visibility definition values into a new generated visibility defintion, to be set on a newly generated affordance
                        visibilityDefinition = new AffordanceVisibilityDefinition(defaultAffordanceVisibilityDefinition);
                        var animationDefinition = new AffordanceAnimationDefinition(defaultAffordanceAnimationDefinition);
                        var generatedAffordanceDefinition = new AffordanceDefinition
                        {
                            control = control,
                            visibilityDefinition = visibilityDefinition,
                            animationDefinition = animationDefinition
                        };

                        generatedAffordanceDefinitions.Add(generatedAffordanceDefinition);
                    }
                    else
                    {
                        visibilityDefinition = matchingAffordanceDefinition.visibilityDefinition;
                    }

                    if (visibilityDefinition != null)
                    {
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
                                var materialsAndAssociatedColors = new List<AffordanceVisualStateData>();
                                // Material, original color, hidden color, animateFromColor(used by animating coroutines, not initialized here)
                                foreach (var material in materialClones)
                                {
                                    // Clones that utilize the standard shader can be cloned and lose their enabled ZWrite value (1), if it was enabled on the material
                                    // Set it again, to avoid ZWrite + transparency visual issues
                                    if (visibilityType != VisibilityControlType.materialSwap && material.HasProperty(k_ZWritePropertyName))
                                        material.SetFloat(k_ZWritePropertyName, 1);

                                    AffordanceVisualStateData materialAndAssociatedColors = null;
                                    switch (visibilityDefinition.visibilityType)
                                    {
                                        case VisibilityControlType.colorProperty:
                                            var originalColor = material.GetColor(visibilityDefinition.colorProperty);
                                            materialAndAssociatedColors = new AffordanceVisualStateData(material, originalColor, hiddenColor, Color.clear, Color.clear);
                                            break;
                                        case VisibilityControlType.alphaProperty:
                                            var originalAlpha = material.GetFloat(shaderAlphaPropety);
                                            var originalAlphaEncodedInColor = Color.white * originalAlpha;
                                            // When animating based on alpha, use the Color.a value of the original, hidden, and animateFrom colors set below
                                            materialAndAssociatedColors = new AffordanceVisualStateData(material, originalAlphaEncodedInColor, hiddenAlphaEncodedInColor, Color.clear, Color.clear);
                                            break;
                                    }

                                    materialsAndAssociatedColors.Add(materialAndAssociatedColors);
                                }

                                visibilityDefinition.visualStateData = materialsAndAssociatedColors;
                            }
                        }
                    }
                }
            }

            if (!affordancesDefinedInMap)
                m_AffordanceMap.AffordanceDefinitions = generatedAffordanceDefinitions.ToArray();

            m_BodyRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(x => !m_AffordanceRenderers.Contains(x) && !IsChildOfProxyOrigin(x.transform)).ToList();

            // Collect renderers not associated with affordances
            // Material swaps don't need to cache original values, only alpha & color
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            switch (bodyVisibilityDefinition.visibilityType)
            {
                case VisibilityControlType.colorProperty:
                    foreach (var renderer in m_BodyRenderers)
                    {
                        var materialClone = MaterialUtils.GetMaterialClone(renderer);
                        if (materialClone != null)
                        {
                            var originalColor = materialClone.GetColor(bodyVisibilityDefinition.colorProperty);
                            if (materialClone.HasProperty(k_ZWritePropertyName))
                                materialClone.SetFloat(k_ZWritePropertyName, 1);

                            m_BodyMaterialOriginalColorMap[materialClone] = new AffordancePropertyTuple<Color>(originalColor, originalColor);
                        }
                    }
                    break;
                case VisibilityControlType.alphaProperty:
                    var shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
                    foreach (var renderer in m_BodyRenderers)
                    {
                        var materialClone = MaterialUtils.GetMaterialClone(renderer);
                        if (materialClone != null)
                        {
                            var originalAlpha = materialClone.GetFloat(shaderAlphaPropety);
                            if (materialClone.HasProperty(k_ZWritePropertyName))
                                materialClone.SetFloat(k_ZWritePropertyName, 1);

                            m_BodyMaterialOriginalAlphaMap[materialClone] = new AffordancePropertyTuple<float>(originalAlpha, originalAlpha);
                        }
                    }
                    break;
                case VisibilityControlType.materialSwap:
                    m_BodySwapOriginalMaterials = new List<Material>();
                    foreach (var renderer in m_BodyRenderers)
                    {
                        var materialClone = MaterialUtils.GetMaterialClone(renderer);
                        m_BodySwapOriginalMaterials.Add(materialClone);
                    }
                    break;
            }
        }

        void Start()
        {
            affordancesVisible = false;
            bodyVisible = false;

            if (m_ProxyAnimator)
            {
                m_ProxyAnimator.Setup(m_AffordanceMap, m_Affordances);
                this.ConnectInterfaces(m_ProxyAnimator, rayOrigin);
            }
        }

        void Update()
        {
            var cameraPosition = CameraUtils.GetMainCamera().transform.position;
            var direction = GetFacingDirection(cameraPosition);
            if (m_FacingDirection != direction)
            {
                m_FacingDirection = direction;
                UpdateFacingDirection(direction);
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();

            foreach (var affordanceDefinition in m_AffordanceMap.AffordanceDefinitions)
            {
                var visibilityDefinition = affordanceDefinition.visibilityDefinition;
                var visibilityType = visibilityDefinition.visibilityType;
                if (visibilityType == VisibilityControlType.colorProperty || visibilityType == VisibilityControlType.alphaProperty)
                {
                    var materialsAndAssociatedColors = visibilityDefinition.visualStateData;
                    if (materialsAndAssociatedColors == null)
                        continue;

                    foreach (var materialToAssociatedColors in visibilityDefinition.visualStateData)
                    {
                        var material = materialToAssociatedColors.originalMaterial;
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

        FacingDirection GetFacingDirection(Vector3 cameraPosition)
        {
            var toCamera = Vector3.Normalize(cameraPosition - transform.position);

            var xDot = Vector3.Dot(toCamera, transform.right);
            var yDot = Vector3.Dot(toCamera, transform.up);
            var zDot = Vector3.Dot(toCamera, transform.forward);

            if (Mathf.Abs(xDot) > Mathf.Abs(yDot))
            {
                if (Mathf.Abs(zDot) > Mathf.Abs(xDot))
                    return zDot > 0 ? FacingDirection.Front : FacingDirection.Back;

                return xDot > 0 ? FacingDirection.Right : FacingDirection.Left;
            }

            if (Mathf.Abs(zDot) > Mathf.Abs(yDot))
                return zDot > 0 ? FacingDirection.Front : FacingDirection.Back;

            return yDot > 0 ? FacingDirection.Top : FacingDirection.Bottom;
        }

        void UpdateFacingDirection(FacingDirection direction)
        {
            foreach (var feedbackRequest in m_FeedbackRequests)
            {
                var request = feedbackRequest.firstElement;
                foreach (var affordance in m_Affordances)
                {
                    if (affordance.control != request.control)
                        continue;

                    foreach (var tooltip in affordance.tooltips)
                    {
                        // Only update placement, do not affect duration
                        this.ShowTooltip(tooltip, true, -1, tooltip.GetPlacement(direction));
                    }
                }
            }
        }

        static IEnumerator AnimateAffordanceColorVisibility(bool isVisible, AffordanceDefinition definition, float fadeInSpeedScalar, float fadeOutSpeedScalar)
        {
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = isVisible ? fadeInSpeedScalar : fadeOutSpeedScalar;
            var currentAmount = 0f;
            var visibilityDefinition = definition.visibilityDefinition;
            var materialsAndColors = visibilityDefinition.visualStateData;
            var shaderColorPropety = visibilityDefinition.colorProperty;

            if (materialsAndColors == null)
                yield break;

            // Setup animateFromColors using the current color values of each material associated with all renderers drawing this affordance
            foreach (var materialAndAssociatedColors in materialsAndColors)
            {
                var animateFromColor = materialAndAssociatedColors.originalMaterial.GetColor(shaderColorPropety); // Get current color from material
                var animateToColor = isVisible ? materialAndAssociatedColors.originalColor : materialAndAssociatedColors.hiddenColor; // (second)original or (third)hidden color(alpha/color.a)
                materialAndAssociatedColors.animateFromColor = animateFromColor;
                materialAndAssociatedColors.animateToColor = animateToColor;
            }

            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime * speedScalar;
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
                foreach (var materialAndAssociatedColors in materialsAndColors)
                {
                    var currentColor = Color.Lerp(materialAndAssociatedColors.animateFromColor, materialAndAssociatedColors.animateToColor, smoothedAmount);
                    materialAndAssociatedColors.originalMaterial.SetColor(shaderColorPropety, currentColor);
                }

                yield return null;
            }
        }

        static IEnumerator AnimateAffordanceAlphaVisibility(bool isVisible, float fadeInSpeedScalar, float fadeOutSpeedScalar, AffordanceVisibilityDefinition visibilityDefinition)
        {
            const float kTargetAmount = 1.1f; // Overshoot in order to force the lerp to blend to maximum value, with needing to set again after while loop
            var speedScalar = isVisible ? fadeInSpeedScalar : fadeOutSpeedScalar;
            var currentAmount = 0f;
            var materialsAndColors = visibilityDefinition.visualStateData;
            var shaderAlphaPropety = visibilityDefinition.alphaProperty;

            // Setup animateFromColors using the current color values of each material associated with all renderers drawing this affordance
            foreach (var materialAndAssociatedColors in materialsAndColors)
            {
                var animateFromAlpha = materialAndAssociatedColors.originalMaterial.GetFloat(shaderAlphaPropety); // Get current alpha from material
                var animateToAlpha = isVisible ? materialAndAssociatedColors.originalColor.a : materialAndAssociatedColors.hiddenColor.a; // (second)original or (third)hidden color(alpha/color.a)
                materialAndAssociatedColors.animateFromColor = Color.white * animateFromAlpha; // Encode the alpha for the FROM color value, color.a
                materialAndAssociatedColors.animateToColor = Color.white * animateToAlpha; // // Encode the alpha for the TO color value, color.a
            }

            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime * speedScalar;
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
                foreach (var materialAndAssociatedColors in materialsAndColors)
                {
                    var currentAlpha = Color.Lerp(materialAndAssociatedColors.animateFromColor, materialAndAssociatedColors.animateToColor, smoothedAmount);
                    materialAndAssociatedColors.originalMaterial.SetFloat(shaderAlphaPropety, currentAlpha.a); // Alpha is encoded in color.a
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
            var speedScalar = isVisible ? m_FadeInSpeedScalar : m_FadeOutSpeedScalar;
            var currentAmount = 0f;
            var shaderColorPropety = bodyVisibilityDefinition.colorProperty;
            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime * speedScalar;
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(currentAmount);
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
            var speedScalar = isVisible ? m_FadeInSpeedScalar : m_FadeOutSpeedScalar;
            var shaderAlphaPropety = bodyVisibilityDefinition.alphaProperty;
            var currentAmount = 0f;
            while (currentAmount < kTargetAmount)
            {
                currentAmount += Time.unscaledDeltaTime * speedScalar;
                var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(speedScalar);
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
            var materialsAndColors = visibilityDefinition.visualStateData;
            for (var i = 0; i < materialsAndColors.Count; ++i)
            {
                var swapMaterial = swapToHiddenMaterial ? visibilityDefinition.hiddenMaterial : materialsAndColors[i].originalMaterial;
                // m_AffordanceRenderers is created/added in sync with the order of the materialsAndAssociatedColors in the affordance visibility definition
                m_AffordanceRenderers[i].material = swapMaterial; // Set swapped material in associated renderer
            }
        }

        void SwapBodyToHiddenMaterial(bool swapToHiddenMaterial)
        {
            var bodyVisibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition;
            for (var i = 0; i < m_BodyRenderers.Count; ++i)
            {
                var swapMaterial = swapToHiddenMaterial ? bodyVisibilityDefinition.hiddenMaterial : m_BodySwapOriginalMaterials[i];
                m_BodyRenderers[i].material = swapMaterial;
            }
        }

        bool IsChildOfProxyOrigin(Transform transform)
        {
            if (transform.IsChildOf(rayOrigin))
                return true;

            if (transform.IsChildOf(menuOrigin))
                return true;

            if (transform.IsChildOf(alternateMenuOrigin))
                return true;

            if (transform.IsChildOf(previewOrigin))
                return true;

            if (transform.IsChildOf(fieldGrabOrigin))
                return true;

            return false;
        }

        public void AddShakeRequest()
        {
            if (m_SemitransparentLockRequest == null)
            {
                AddFeedbackRequest(m_ShakeFeedbackRequest);
            }
        }

        public void AddFeedbackRequest(ProxyFeedbackRequest proxyRequest)
        {
            Coroutine monitoringCoroutine = null;
            if (isActiveAndEnabled)
                monitoringCoroutine = StartCoroutine(MonitorFeedbackRequestLifespan(proxyRequest));

            m_FeedbackRequests.Add(new FeedbackRequestTuple(proxyRequest, monitoringCoroutine));

            ExecuteFeedback(proxyRequest);
        }

        void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
        {
            if (!isActiveAndEnabled)
                return;

            foreach (var affordance in m_Affordances)
            {
                if (affordance.control != changedRequest.control)
                    continue;

                ProxyFeedbackRequest request = null;
                foreach (var requestCoroutineTuple in m_FeedbackRequests)
                {
                    var feedbackRequest = requestCoroutineTuple.firstElement;
                    if (feedbackRequest.control != affordance.control)
                        continue;

                    if (request == null || feedbackRequest.priority >= request.priority)
                        request = feedbackRequest;
                }

                if (request == null)
                    continue;

                if (affordance.renderer)
                    this.SetHighlight(affordance.renderer.gameObject, !request.suppressExisting, duration: k_DefaultFeedbackDuration);

                var tooltipText = request.tooltipText;
                if (!string.IsNullOrEmpty(tooltipText) || request.suppressExisting)
                {
                    foreach (var tooltip in affordance.tooltips)
                    {
                        if (tooltip)
                        {
                            tooltip.tooltipText = tooltipText;
                            this.ShowTooltip(tooltip, true, k_DefaultFeedbackDuration,
                                tooltip.GetPlacement(m_FacingDirection));
                        }
                    }
                }
            }

            UpdateVisibility();
        }

        public void RemoveFeedbackRequest(ProxyFeedbackRequest request)
        {
            foreach (var affordance in m_Affordances)
            {
                if (affordance.control != request.control)
                    continue;

                if (affordance.renderer)
                    this.SetHighlight(affordance.renderer.gameObject, false);

                foreach (var tooltip in affordance.tooltips)
                {
                    if (tooltip)
                    {
                        tooltip.tooltipText = string.Empty;
                        this.HideTooltip(tooltip, true);
                    }
                }
            }

            foreach (var tuple in m_FeedbackRequests)
            {
                if (tuple.firstElement == request)
                {
                    m_FeedbackRequests.Remove(tuple);
                    ExecuteFeedback(request);
                    break;
                }
            }
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            k_FeedbackRequestsCopy.Clear();
            foreach (var request in m_FeedbackRequests)
            {
                if (request.firstElement.caller == caller)
                    k_FeedbackRequestsCopy.Add(request);
            }

            foreach (var request in k_FeedbackRequestsCopy)
            {
                RemoveFeedbackRequest(request.firstElement);
            }
        }

        public void UpdateVisibility()
        {
            var shakenVisibility = m_SemitransparentLockRequest != null;
            // Proxy affordances should be visible when the input-device is shaken
            var proxyRequestsExist = shakenVisibility;

            if (!shakenVisibility)
            {
                // Find any visible feedback requests for each hand
                foreach (var requestCoroutineTuple in m_FeedbackRequests)
                {
                    var request = requestCoroutineTuple.firstElement;
                    proxyRequestsExist = request.visible && request.node == Node.RightHand;

                    if (proxyRequestsExist)
                        break;
                }
            }

            affordancesVisible = proxyRequestsExist;
            bodyVisible = shakenVisibility;
        }

        IEnumerator MonitorFeedbackRequestLifespan(ProxyFeedbackRequest request)
        {
            if (request.proxyShaken)
            {
                if (m_SemitransparentLockRequest != null)
                    yield break;

                m_SemitransparentLockRequest = request;
            }

            request.visible = true;

            const float kShakenVisibilityDuration = 5f;
            const float kShorterOpaqueDurationScalar = 0.125f;
            float duration = request.proxyShaken ? kShakenVisibilityDuration : k_DefaultFeedbackDuration * kShorterOpaqueDurationScalar;
            var currentDuration = 0f;
            while (currentDuration < duration)
            {
                currentDuration += Time.unscaledDeltaTime;
                yield return null;
            }

            request.visible = false;

            // Unlock shaken body visibility if this was the most recent request the trigger the full body visibility
            if (m_SemitransparentLockRequest == request)
                m_SemitransparentLockRequest = null;

            UpdateVisibility();
        }
    }
}
#endif
