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

    /// <summary>
    /// ProxyFeedbackRequests reside in feedbackRequest collection until the action associated with an affordance changes
    /// Some are removed immediately after being added; others exist for the duration of an action/tool's lifespan
    /// </summary>
    public class ProxyFeedbackRequest : FeedbackRequest
    {
        public int priority;
        public VRControl control;
        public Node node;
        public string tooltipText;
        public bool suppressExisting;
        public bool showBody;
        public float druation = 5f;
    }

    class ProxyNode : MonoBehaviour, ISetTooltipVisibility, ISetHighlight, IConnectInterfaces
    {
        class AffordanceData
        {
            class MaterialData
            {
                public Material originalMaterial;
                public Material material;
                public Color originalColor;
                public Color currentColor;
            }

            readonly List<Renderer> m_Renderers = new List<Renderer>();
            readonly AffordanceDefinition m_Definition;
            readonly List<AffordanceTooltip> m_Tooltips = new List<AffordanceTooltip>();
            readonly ProxyNode m_Owner;

            bool m_Visible;
            float m_VisibleDuration;

            bool m_WasVisible;
            float m_VisibleChangeTime;

            readonly List<Tuple<Renderer, MaterialData[]>> m_MaterialData = new List<Tuple<Renderer, MaterialData[]>>();

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

                    Color originalColor;
                    switch (visibilityType)
                    {
                        case VisibilityControlType.ColorProperty:
                            originalColor = material.GetColor(visibilityDefinition.colorProperty);
                            materialData[i] = new MaterialData
                            {
                                material = material,
                                originalMaterial = originalMaterial,
                                currentColor = originalColor,
                                originalColor = originalColor
                            };
                            break;
                        case VisibilityControlType.AlphaProperty:
                            originalColor = material.GetFloat(visibilityDefinition.alphaProperty) * Color.white;
                            materialData[i] = new MaterialData
                            {
                                material = material,
                                originalMaterial = originalMaterial,
                                currentColor = originalColor,
                                originalColor = originalColor
                            };
                            break;
                    }
                }
            }

            public void SetVisible(bool visible, float visibleDuration = 0f)
            {
                m_Visible = visible;
                m_VisibleDuration = visibleDuration;
                m_VisibleChangeTime = Time.time;
            }

            public void Update(float fadeInAmount, float fadeOutAmount, bool bodyVisible = false)
            {
                var visible = m_Visible || bodyVisible;
                var visibilityDefinition = definition.visibilityDefinition;

                switch (visibilityDefinition.visibilityType)
                {
                    case VisibilityControlType.AlphaProperty:
                        foreach (var materialData in m_MaterialData)
                        {
                            foreach (var material in materialData.secondElement)
                            {
                                var currentColor = material.currentColor;
                                var current = currentColor.a;
                                var target = visible ? material.originalColor.a : visibilityDefinition.hiddenColor.a;
                                if (!Mathf.Approximately(current, target))
                                {
                                    if (current > target)
                                    {
                                        current -= fadeOutAmount;
                                        if (current < target)
                                            current = target;
                                    }
                                    else
                                    {
                                        current += fadeInAmount;
                                        if (current > target)
                                            current = target;
                                    }

                                    currentColor.a = current;
                                    material.material.SetFloat(visibilityDefinition.alphaProperty, current);
                                    material.currentColor = currentColor;
                                }
                            }
                        }
                        break;
                    case VisibilityControlType.ColorProperty:
                        foreach (var materialData in m_MaterialData)
                        {
                            foreach (var material in materialData.secondElement)
                            {
                                var currentColor = material.currentColor;
                                var targetColor = visible ? material.originalColor : visibilityDefinition.hiddenColor;
                                var change = false;
                                for (var i = 0; i < 4; i++)
                                {
                                    var current = currentColor[i];
                                    var target = targetColor[i];

                                    if (Mathf.Approximately(current, target))
                                        continue;

                                    if (current > target)
                                    {
                                        current -= fadeOutAmount;
                                        if (current < target)
                                            current = target;
                                    }
                                    else
                                    {
                                        current += fadeInAmount;
                                        if (current > target)
                                            current = target;
                                    }

                                    currentColor[i] = current;
                                    change = true;
                                }

                                if (change)
                                {
                                    material.material.SetColor(visibilityDefinition.colorProperty, currentColor);
                                    material.currentColor = currentColor;
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

                if (m_Visible && Time.time - m_VisibleChangeTime > m_VisibleDuration)
                {
                    m_Visible = false;
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

        static readonly ProxyFeedbackRequest k_ShakeFeedbackRequest = new ProxyFeedbackRequest { showBody = true };

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

            var fadeInAmount = m_FadeInSpeedScalar * Time.deltaTime;
            var fadeOutAmount = m_FadeOutSpeedScalar * Time.deltaTime;

            foreach (var kvp in m_AffordanceData)
            {
                kvp.Value.Update(fadeInAmount, fadeOutAmount);
            }

            m_BodyData.Update(fadeInAmount, fadeOutAmount);
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
            var transform = m_NaturalOrientation;
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
                var druation = changedRequest.druation;
                foreach (var kvp in m_AffordanceData)
                {
                    kvp.Value.SetVisible(true, druation);
                }

                m_BodyData.SetVisible(true, druation);
                return;
            }

            AffordanceData affordanceData;
            if (m_AffordanceData.TryGetValue(changedRequest.control, out affordanceData))
            {
                ProxyFeedbackRequest request = null;
                foreach (var feedbackRequest in m_FeedbackRequests)
                {
                    if (feedbackRequest.control != changedRequest.control)
                        continue;

                    if (request == null || feedbackRequest.priority >= request.priority)
                        request = feedbackRequest;
                }

                if (request == null)
                    return;

                affordanceData.SetVisible(true, request.druation);

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

                affordanceData.SetVisible(false);

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
