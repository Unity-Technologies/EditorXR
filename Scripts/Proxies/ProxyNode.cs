#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    using VisibilityControlType = ProxyAffordanceMap.VisibilityControlType;
    using VRControl = VRInputDevice.VRControl;

    class ProxyNode : MonoBehaviour, ISetTooltipVisibility, ISetHighlight, IConnectInterfaces
    {
        class AffordanceData
        {
            class MaterialData
            {
                public Material originalMaterial;
                public Material material;
                public Color originalColor;
                public Color startColor;
            }

            readonly List<Renderer> m_Renderers = new List<Renderer>();
            readonly AffordanceDefinition m_Definition;
            readonly List<AffordanceTooltip> m_Tooltips = new List<AffordanceTooltip>();
            readonly ProxyNode m_Owner;

            bool m_WasVisible;
            float m_VisibleChangeTime;

            readonly List<Tuple<Renderer, MaterialData[]>> m_MaterialData = new List<Tuple<Renderer, MaterialData[]>>();

            public bool visible { get; set; }
            public float visibleDuration { get; set; }
            public List<Renderer> renderers { get { return m_Renderers; } }
            public AffordanceDefinition definition { get { return m_Definition; } }
            public List<AffordanceTooltip> tooltips { get { return m_Tooltips; } }

            public AffordanceData(AffordanceDefinition definition, ProxyNode owner)
            {
                m_Definition = definition;
                m_Owner = owner;
            }

            public void AddAffordance(Renderer renderer, AffordanceTooltip[] tooltips)
            {
                if (tooltips != null)
                    m_Tooltips.AddRange(tooltips);

                m_Renderers.Add(renderer);
                var originalMaterials = renderer.sharedMaterials;
                var materials = originalMaterials;
                var visibilityDefinition = definition.visibilityDefinition;
                var materialData = new MaterialData[originalMaterials.Length];
                var materialPairs = new Tuple<Renderer, MaterialData[]>(renderer, materialData);
                m_MaterialData.Add(materialPairs);
                var visibilityType = visibilityDefinition.visibilityType;

                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    var originalMaterial = originalMaterials[i];

                    var originalColor = default(Color);
                    switch (visibilityType)
                    {
                        case VisibilityControlType.AlphaProperty:
                            originalColor = material.GetFloat(visibilityDefinition.alphaProperty) * Color.white;
                            break;
                        case VisibilityControlType.ColorProperty:
                            originalColor = material.GetColor(visibilityDefinition.colorProperty);
                            break;
                    }

                    materialData[i] = new MaterialData
                    {
                        material = material,
                        originalMaterial = originalMaterial,
                        startColor = originalColor,
                        originalColor = originalColor
                    };
                }
            }

            public void Update(float fadeInDuration, float fadeOutDuration, bool visibilityOverride = false)
            {
                var time = Time.time;
                var visible = this.visible || visibilityOverride;
                if (visible != m_WasVisible)
                    m_VisibleChangeTime = time;

                var timeDiff = time - m_VisibleChangeTime;
                var fadeDuration = visible ? fadeInDuration : fadeOutDuration;

                var visibilityDefinition = definition.visibilityDefinition;
                switch (visibilityDefinition.visibilityType)
                {
                    case VisibilityControlType.AlphaProperty:
                        foreach (var materialPair in m_MaterialData)
                        {
                            foreach (var materialData in materialPair.secondElement)
                            {
                                var material = materialData.material;
                                var alphaProperty = visibilityDefinition.alphaProperty;
                                if (visible != m_WasVisible)
                                    materialData.startColor = material.GetFloat(alphaProperty) * Color.white;

                                var startColor = materialData.startColor;
                                var current = startColor.a;
                                var target = visible ? materialData.originalColor.a : visibilityDefinition.hiddenColor.a;
                                if (!Mathf.Approximately(current, target))
                                {
                                    var duration = current / target * fadeDuration;
                                    var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(timeDiff / duration);
                                    if (smoothedAmount > 1)
                                    {
                                        current = target;
                                        startColor.a = current;
                                        materialData.startColor = startColor;
                                    }
                                    else
                                    {
                                        current = Mathf.Lerp(current, target, smoothedAmount);
                                    }

                                    material.SetFloat(alphaProperty, current);
                                }
                            }
                        }
                        break;
                    case VisibilityControlType.ColorProperty:
                        foreach (var materialPair in m_MaterialData)
                        {
                            foreach (var materialData in materialPair.secondElement)
                            {
                                var material = materialData.material;
                                var colorProperty = visibilityDefinition.colorProperty;
                                if (visible != m_WasVisible)
                                    materialData.startColor = material.GetColor(colorProperty);

                                var startColor = materialData.startColor;
                                var targetColor = visible ? materialData.originalColor : visibilityDefinition.hiddenColor;
                                if (startColor != targetColor)
                                {
                                    var duration = startColor.grayscale / targetColor.grayscale * fadeDuration;
                                    var smoothedAmount = MathUtilsExt.SmoothInOutLerpFloat(timeDiff / duration);
                                    if (smoothedAmount > 1)
                                    {
                                        startColor = targetColor;
                                        materialData.startColor = startColor;
                                    }
                                    else
                                    {
                                        startColor = Color.Lerp(startColor, targetColor, smoothedAmount);
                                    }

                                    material.SetColor(colorProperty, startColor);
                                }
                            }
                        }
                        break;
                    case VisibilityControlType.MaterialSwap:
                        if (visible != m_WasVisible)
                        {
                            foreach (var materialData in m_MaterialData)
                            {
                                var materials = materialData.firstElement.sharedMaterials;
                                var materialDatas = materialData.secondElement;
                                for (var i = 0; i < materialDatas.Length; i++)
                                {
                                    var data = materialDatas[i];
                                    materials[i] = visible ? data.originalMaterial : data.material;
                                }
                            }
                        }
                        break;
                }

                m_WasVisible = visible;

                if (this.visible && visibleDuration >= 0 && timeDiff > visibleDuration)
                {
                    this.visible = false;
                    foreach (var materialData in m_MaterialData)
                    {
                        var renderer = materialData.firstElement;
                        if (renderer)
                            m_Owner.SetHighlight(renderer.gameObject, false);
                    }

                    if (tooltips != null)
                    {
                        foreach (var tooltip in tooltips)
                        {
                            if (tooltip)
                                m_Owner.HideTooltip(tooltip, true);
                        }
                    }
                }
            }

            public void OnDestroy()
            {
                foreach (var materialData in m_MaterialData)
                {
                    foreach (var material in materialData.secondElement)
                    {
                        ObjectUtils.Destroy(material.material);
                    }
                }
            }
        }

        const string k_ZWritePropertyName = "_ZWrite";

        static readonly ProxyFeedbackRequest k_ShakeFeedbackRequest = new ProxyFeedbackRequest { showBody = true };

        [SerializeField]
        float m_FadeInDuration = 0.5f;

        [SerializeField]
        float m_FadeOutDuration = 2f;

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
        Transform m_NaturalOrientation;

        [SerializeField]
        ProxyAnimator m_ProxyAnimator;

        [SerializeField]
        ProxyAffordanceMap m_AffordanceMap;

        [HideInInspector]
        [SerializeField]
        Material m_ProxyBackgroundMaterial;

        [Tooltip("Affordance objects that store transform, renderer, and tooltip references")]
        [SerializeField]
        Affordance[] m_Affordances;

        readonly Dictionary<VRControl, AffordanceData> m_AffordanceData = new Dictionary<VRControl, AffordanceData>();
        AffordanceData m_BodyData;

        FacingDirection m_FacingDirection = FacingDirection.Back;

        readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();

        // Local method use only -- created here to reduce garbage collection
        static readonly List<ProxyFeedbackRequest> k_FeedbackRequestsCopy = new List<ProxyFeedbackRequest>();

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

        void Awake()
        {
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

            var affordanceMapDefinitions = m_AffordanceMap.AffordanceDefinitions;
            var affordanceRenderers = new HashSet<Renderer>();
            var defaultAffordanceVisibilityDefinition = m_AffordanceMap.defaultAffordanceVisibilityDefinition;
            var defaultAffordanceAnimationDefinition = m_AffordanceMap.defaultAnimationDefinition;
            foreach (var affordance in m_Affordances)
            {
                var control = affordance.control;
                AffordanceData affordanceData;
                if (!m_AffordanceData.TryGetValue(control, out affordanceData))
                {
                    var affordanceDefinition = affordanceMapDefinitions.FirstOrDefault(x => x.control == control);
                    if (affordanceDefinition == null)
                    {
                        affordanceDefinition = new AffordanceDefinition
                        {
                            control = control,
                            visibilityDefinition = defaultAffordanceVisibilityDefinition,
                            animationDefinition = defaultAffordanceAnimationDefinition
                        };
                    }

                    affordanceData = new AffordanceData(affordanceDefinition, this);
                    m_AffordanceData[control] = affordanceData;
                }

                var renderer = affordance.renderer;
                if (affordanceRenderers.Add(renderer))
                {
                    MaterialUtils.CloneMaterials(renderer); // Clone all materials associated with each renderer once

                    // Clones that utilize the standard shader can lose their enabled ZWrite value (1), if it was enabled on the material
                    foreach (var material in renderer.sharedMaterials)
                    {
                        material.SetFloat(k_ZWritePropertyName, 1);
                    }
                }

                affordanceData.AddAffordance(renderer, affordance.tooltips);
            }

            foreach (var renderer in affordanceRenderers)
            {
                renderer.AddMaterial(m_ProxyBackgroundMaterial);
            }

            var bodyRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(x => !affordanceRenderers.Contains(x) && !IsChildOfProxyOrigin(x.transform)).ToList();

            var bodyAffordanceDefinition = new AffordanceDefinition
            {
                visibilityDefinition = m_AffordanceMap.bodyVisibilityDefinition
            };

            m_BodyData = new AffordanceData(bodyAffordanceDefinition, this);
            foreach (var renderer in bodyRenderers)
            {
                MaterialUtils.CloneMaterials(renderer);
                m_BodyData.AddAffordance(renderer, null);
                renderer.AddMaterial(m_ProxyBackgroundMaterial);
            }
        }

        void Start()
        {
            if (m_ProxyAnimator)
            {
                m_ProxyAnimator.Setup(m_AffordanceData.Select(data => data.Value.definition).ToArray(), m_Affordances);
                this.ConnectInterfaces(m_ProxyAnimator, rayOrigin);
            }

            AddFeedbackRequest(k_ShakeFeedbackRequest);
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

            var bodyVisible = m_BodyData.visible;
            foreach (var kvp in m_AffordanceData)
            {
                kvp.Value.Update(m_FadeInDuration, m_FadeOutDuration, bodyVisible);
            }

            m_BodyData.Update(m_FadeInDuration, m_FadeOutDuration);
        }

        void OnDestroy()
        {
            StopAllCoroutines();

            foreach (var kvp in m_AffordanceData)
            {
                kvp.Value.OnDestroy();
            }

            m_BodyData.OnDestroy();
        }

        FacingDirection GetFacingDirection(Vector3 cameraPosition)
        {
            var toCamera = Vector3.Normalize(cameraPosition - m_NaturalOrientation.position);

            var xDot = Vector3.Dot(toCamera, m_NaturalOrientation.right);
            var yDot = Vector3.Dot(toCamera, m_NaturalOrientation.up);
            var zDot = Vector3.Dot(toCamera, m_NaturalOrientation.forward);

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
            foreach (var request in m_FeedbackRequests)
            {
                AffordanceData affordanceData;
                if (m_AffordanceData.TryGetValue(request.control, out affordanceData))
                {
                    foreach (var tooltip in affordanceData.tooltips)
                    {
                        // Only update placement, do not affect duration
                        this.ShowTooltip(tooltip, true, -1, tooltip.GetPlacement(direction));
                    }
                }
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
            RemoveFeedbackRequest(k_ShakeFeedbackRequest);
            AddFeedbackRequest(k_ShakeFeedbackRequest);
        }

        public void AddFeedbackRequest(ProxyFeedbackRequest request)
        {
            m_FeedbackRequests.Add(request);
            ExecuteFeedback(request);
        }

        void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
        {
            if (!isActiveAndEnabled)
                return;

            if (changedRequest.showBody)
            {
                m_BodyData.visible = true;
                m_BodyData.visibleDuration = changedRequest.duration;
                return;
            }

            AffordanceData affordanceData;
            if (m_AffordanceData.TryGetValue(changedRequest.control, out affordanceData))
            {
                ProxyFeedbackRequest request = null;
                foreach (var feedbackRequest in m_FeedbackRequests)
                {
                    if (feedbackRequest.control != changedRequest.control || feedbackRequest.showBody != changedRequest.showBody)
                        continue;

                    if (request == null || feedbackRequest.priority >= request.priority)
                        request = feedbackRequest;
                }

                if (request == null)
                    return;

                affordanceData.visible = !request.suppressExisting;
                affordanceData.visibleDuration = request.duration;

                foreach (var renderer in affordanceData.renderers)
                {
                    if (renderer)
                        this.SetHighlight(renderer.gameObject, !request.suppressExisting);
                }

                var tooltipText = request.tooltipText;
                if (!string.IsNullOrEmpty(tooltipText) || request.suppressExisting)
                {
                    foreach (var tooltip in affordanceData.tooltips)
                    {
                        if (tooltip)
                        {
                            tooltip.tooltipText = tooltipText;
                            this.ShowTooltip(tooltip, true, placement: tooltip.GetPlacement(m_FacingDirection));
                        }
                    }
                }
            }
        }

        public void RemoveFeedbackRequest(ProxyFeedbackRequest request)
        {
            AffordanceData affordanceData;
            if (m_AffordanceData.TryGetValue(request.control, out affordanceData))
            {
                foreach (var renderer in affordanceData.renderers)
                {
                    if (renderer)
                        this.SetHighlight(renderer.gameObject, false);
                }

                affordanceData.visible = false;

                if (!string.IsNullOrEmpty(request.tooltipText))
                {
                    foreach (var tooltip in affordanceData.tooltips)
                    {
                        if (tooltip)
                        {
                            tooltip.tooltipText = string.Empty;
                            this.HideTooltip(tooltip, true);
                        }
                    }
                }
            }

            foreach (var feedbackRequest in m_FeedbackRequests)
            {
                if (feedbackRequest == request)
                {
                    m_FeedbackRequests.Remove(feedbackRequest);
                    if (!request.showBody)
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
                if (request.caller == caller)
                    k_FeedbackRequestsCopy.Add(request);
            }

            foreach (var request in k_FeedbackRequestsCopy)
            {
                RemoveFeedbackRequest(request);
            }
        }
    }
}
#endif
