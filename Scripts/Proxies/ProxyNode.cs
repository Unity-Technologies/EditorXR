
using System;
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
                class VisibilityState
                {
                    readonly int m_MaterialIndex;
                    readonly Material m_Material;
                    readonly AffordanceTooltip[] m_Tooltips;
                    readonly AffordanceVisibilityDefinition m_Definition;

                    readonly Action<float> m_SetFloat;
                    readonly Action<Color> m_SetColor;

                    bool m_Visibile;
                    float m_VisibilityChangeTime;

                    public int materialIndex { get { return m_MaterialIndex; } }
                    public AffordanceTooltip[] tooltips { get { return m_Tooltips; } }
                    public AffordanceVisibilityDefinition definition { get { return m_Definition; } }

                    public float visibleDuration { get; set; }
                    public float hideTime { get { return m_VisibilityChangeTime + visibleDuration; } }

                    public Action<float> setFloat { get { return m_SetFloat; } }
                    public Action<Color> setColor { get { return m_SetColor; } }

                    public bool visible
                    {
                        get { return m_Visibile; }
                        set
                        {
                            m_VisibilityChangeTime = Time.time;
                            m_Visibile = value;
                        }
                    }

                    public VisibilityState(Renderer renderer, AffordanceTooltip[] tooltips, AffordanceVisibilityDefinition definition, Material material)
                    {
                        m_Tooltips = tooltips;
                        m_Definition = definition;
                        m_Material = material;
                        m_MaterialIndex = Array.IndexOf(renderer.sharedMaterials, material);

                        // Cache delegate versions of SetFloat and SetColor to avoid GC when used in AnimateProperty
                        m_SetFloat = SetFloat;
                        m_SetColor = SetColor;
                    }

                    void SetFloat(float value)
                    {
                        m_Material.SetFloat(m_Definition.alphaProperty, value);
                    }

                    void SetColor(Color value)
                    {
                        m_Material.SetColor(m_Definition.colorProperty, value);
                    }
                }

                bool m_WasVisible;
                float m_VisibleChangeTime;
                Color m_OriginalColor;
                Color m_StartColor;
                Color m_CurrentColor;

                readonly Dictionary<int, VisibilityState> m_AffordanceVisibilityStates = new Dictionary<int, VisibilityState>();
                readonly Dictionary<KeyValuePair<Material, string>, VisibilityState> m_VisibilityStates = new Dictionary<KeyValuePair<Material, string>, VisibilityState>();

                public void AddAffordance(Material material, VRControl control, Renderer renderer,
                    AffordanceTooltip[] tooltips, AffordanceVisibilityDefinition definition)
                {
                    var key = (int)control;

                    if (m_AffordanceVisibilityStates.ContainsKey(key))
                        Debug.LogWarning("Multiple affordaces added to " + this + " for " + control);

                    m_AffordanceVisibilityStates[key] = new VisibilityState(renderer, tooltips, definition, material);

                    switch (definition.visibilityType)
                    {
                        case VisibilityControlType.AlphaProperty:
                            m_OriginalColor = material.GetFloat(definition.alphaProperty) * Color.white;
                            break;
                        case VisibilityControlType.ColorProperty:
                            m_OriginalColor = material.GetColor(definition.colorProperty);
                            break;
                    }

                    m_StartColor = m_OriginalColor;
                    m_CurrentColor = m_StartColor;
                }

                public void Update(Renderer renderer, Material material, float time, float fadeInDuration, float fadeOutDuration,
                    ProxyNode proxyNode, AffordanceVisibilityDefinition visibilityOverride)
                {
                    VisibilityState visibilityState = null;
                    var definition = visibilityOverride;
                    var hideTime = 0f;
                    if (definition == null)
                    {
                        foreach (var kvp in m_AffordanceVisibilityStates)
                        {
                            var state = kvp.Value;
                            if (state.visible)
                            {
                                if (state.hideTime > hideTime)
                                {
                                    definition = state.definition;
                                    hideTime = state.visibleDuration > 0 ? state.hideTime : 0;
                                    visibilityState = state;
                                }
                            }
                        }
                    }

                    var visible = definition != null;
                    if (!visible)
                    {
                        foreach (var kvp in m_AffordanceVisibilityStates)
                        {
                            visibilityState = kvp.Value;
                            definition = visibilityState.definition;
                            break;
                        }
                    }

                    var fadeDuration = visible ? fadeInDuration : fadeOutDuration;
                    switch (definition.visibilityType)
                    {
                        case VisibilityControlType.AlphaProperty:
                            if (visibilityState == null)
                            {
                                var kvp = new KeyValuePair<Material, string>(material, definition.alphaProperty);
                                if (!m_VisibilityStates.TryGetValue(kvp, out visibilityState))
                                {
                                    visibilityState = new VisibilityState(renderer, null, definition, material);
                                    m_VisibilityStates[kvp] = visibilityState;
                                }
                            }

                            TransitionUtils.AnimateProperty(time, visible, ref m_WasVisible, ref m_VisibleChangeTime,
                                ref m_CurrentColor.a, ref m_StartColor.a, definition.hiddenColor.a, m_OriginalColor.a,
                                fadeDuration, Mathf.Approximately, TransitionUtils.GetPercentage, Mathf.Lerp,
                                visibilityState.setFloat, false);
                            break;
                        case VisibilityControlType.ColorProperty:
                            if (visibilityState == null)
                            {
                                var kvp = new KeyValuePair<Material, string>(material, definition.alphaProperty);
                                if (!m_VisibilityStates.TryGetValue(kvp, out visibilityState))
                                {
                                    visibilityState = new VisibilityState(renderer, null, definition, material);
                                    m_VisibilityStates[kvp] = visibilityState;
                                }
                            }

                            TransitionUtils.AnimateProperty(time, visible, ref m_WasVisible, ref m_VisibleChangeTime,
                                ref m_CurrentColor, ref m_StartColor, definition.hiddenColor, m_OriginalColor,
                                fadeDuration, TransitionUtils.Approximately, TransitionUtils.GetPercentage, Color.Lerp,
                                visibilityState.setColor, false);
                            break;
                    }

                    if (visible != m_WasVisible)
                    {
                        foreach (var kvp in m_AffordanceVisibilityStates)
                        {
                            visibilityState = kvp.Value;
                            if (visibilityState.definition.visibilityType == VisibilityControlType.MaterialSwap)
                                renderer.sharedMaterials[visibilityState.materialIndex] =
                                    visible ? material : visibilityState.definition.hiddenMaterial;
                        }
                    }

                    m_WasVisible = visible;

                    if (visible && hideTime > 0 && Time.time > hideTime)
                    {
                        foreach (var kvp in m_AffordanceVisibilityStates)
                        {
                            visibilityState = kvp.Value;
                            var tooltips = visibilityState.tooltips;
                            if (tooltips != null)
                            {
                                foreach (var tooltip in tooltips)
                                {
                                    if (tooltip)
                                        proxyNode.HideTooltip(tooltip, true);
                                }
                            }

                            proxyNode.SetHighlight(renderer.gameObject, false);

                            visibilityState.visible = false;
                        }
                    }
                }

                public bool GetVisibility(VRControl control)
                {
                    foreach (var kvp in m_AffordanceVisibilityStates)
                    {
                        if (kvp.Key != (int)control)
                            continue;

                        if (kvp.Value.visible)
                            return true;
                    }

                    return false;
                }

                public void SetVisibility(bool visible, float duration, VRControl control)
                {
                    VisibilityState visibilityState;
                    if (m_AffordanceVisibilityStates.TryGetValue((int)control, out visibilityState))
                    {
                        visibilityState.visible = visible;
                        visibilityState.visibleDuration = duration;
                    }
                }
            }

            readonly Dictionary<Material, MaterialData> m_MaterialDictionary = new Dictionary<Material, MaterialData>();

            public void AddAffordance(Affordance affordance, AffordanceVisibilityDefinition[] definitions)
            {
                var control = affordance.control;
                var materials = affordance.materials;
                var renderers = affordance.renderers;
                var tooltips = affordance.tooltips;
                if (materials != null)
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        AddMaterialData(materials[i], control, renderers[i], tooltips, definitions[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < renderers.Length; i++)
                    {
                        var renderer = renderers[i];
                        foreach (var material in renderer.sharedMaterials)
                        {
                            AddMaterialData(material, control, renderer, tooltips, definitions[i]);
                        }
                    }
                }
            }

            public void AddRenderer(Renderer renderer, AffordanceVisibilityDefinition definition)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    AddMaterialData(material, default(VRControl), renderer, null, definition);
                }
            }

            void AddMaterialData(Material material, VRControl control, Renderer renderer, AffordanceTooltip[] tooltips,
                AffordanceVisibilityDefinition definition)
            {
                MaterialData materialData;
                if (!m_MaterialDictionary.TryGetValue(material, out materialData))
                {
                    materialData = new MaterialData();
                    m_MaterialDictionary[material] = materialData;
                }

                materialData.AddAffordance(material, control, renderer, tooltips, definition);
            }

            public void SetVisibility(bool visible, float duration = 0f, VRControl control = default(VRControl))
            {
                foreach (var kvp in m_MaterialDictionary)
                {
                    kvp.Value.SetVisibility(visible, duration, control);
                }
            }

            public bool GetVisibility(VRControl control = default(VRControl))
            {
                foreach (var kvp in m_MaterialDictionary)
                {
                    if (kvp.Value.GetVisibility(control))
                        return true;
                }

                return false;
            }

            public void Update(Renderer renderer, float time, float fadeInDuration, float fadeOutDuration,
                ProxyNode proxyNode, AffordanceVisibilityDefinition visibilityOverride = null)
            {
                foreach (var kvp in m_MaterialDictionary)
                {
                    kvp.Value.Update(renderer, kvp.Key, time, fadeInDuration, fadeOutDuration, proxyNode, visibilityOverride);
                }
            }

            public void OnDestroy()
            {
                foreach (var kvp in m_MaterialDictionary)
                {
                    ObjectUtils.Destroy(kvp.Key);
                }
            }
        }

        const string k_ZWritePropertyName = "_ZWrite";
        const float k_LastFacingAngleWeight = 0.1f;         // How much extra emphasis to give the last facing angle to prevent 'jitter' when looking at a controller on a boundary

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

        readonly Dictionary<Renderer, AffordanceData> m_AffordanceData = new Dictionary<Renderer, AffordanceData>();
        readonly List<Tuple<Renderer, AffordanceData>> m_BodyData = new List<Tuple<Renderer, AffordanceData>>();

        FacingDirection m_FacingDirection = FacingDirection.Back;

        SerializedProxyNodeFeedback m_SerializedFeedback;
        readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();
        readonly Dictionary<RequestKey, RequestData> m_RequestData = new Dictionary<RequestKey, RequestData>();

        Vector3 m_FacingAngleWeights = Vector3.one;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<ProxyFeedbackRequest> k_FeedbackRequestsCopy = new List<ProxyFeedbackRequest>();
        readonly Queue<RequestKey> m_RequestKeyPool = new Queue<RequestKey>();

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

            var affordanceDefinitions = new AffordanceDefinition[m_Affordances.Length];
            var affordanceMapDefinitions = m_AffordanceMap.AffordanceDefinitions;
            var defaultAffordanceVisibilityDefinition = m_AffordanceMap.defaultAffordanceVisibilityDefinition;
            var defaultAffordanceAnimationDefinition = m_AffordanceMap.defaultAnimationDefinition;
            for (var i = 0; i < m_Affordances.Length; i++)
            {
                var affordance = m_Affordances[i];
                var renderers = affordance.renderers;
                foreach (var renderer in renderers)
                {
                    var sharedMaterials = renderer.sharedMaterials;
                    AffordanceData affordanceData;
                    if (!m_AffordanceData.TryGetValue(renderer, out affordanceData))
                    {
                        MaterialUtils.CloneMaterials(renderer); // Clone all materials associated with each renderer once
                        affordanceData = new AffordanceData();
                        m_AffordanceData[renderer] = affordanceData;

                        // Clones that utilize the standard shader can lose their enabled ZWrite value (1), if it was enabled on the material
                        foreach (var material in sharedMaterials)
                        {
                            material.SetFloat(k_ZWritePropertyName, 1);
                        }
                    }

                    var control = affordance.control;
                    var definition = affordanceMapDefinitions.FirstOrDefault(x => x.control == control);
                    if (definition == null)
                    {
                        definition = new AffordanceDefinition
                        {
                            control = control,
                            visibilityDefinitions = new[] { defaultAffordanceVisibilityDefinition },
                            animationDefinitions = new[] { defaultAffordanceAnimationDefinition }
                        };
                    }

                    affordanceDefinitions[i] = definition;
                    affordanceData.AddAffordance(affordance, definition.visibilityDefinitions);
                }
            }

            foreach (var kvp in m_AffordanceData)
            {
                kvp.Key.AddMaterial(m_ProxyBackgroundMaterial);
            }

            var bodyRenderers = GetComponentsInChildren<Renderer>(true)
                .Where(x => !m_AffordanceData.ContainsKey(x) && !IsChildOfProxyOrigin(x.transform)).ToList();

            foreach (var renderer in bodyRenderers)
            {
                MaterialUtils.CloneMaterials(renderer);
                var affordanceData = new AffordanceData();
                m_BodyData.Add(new Tuple<Renderer, AffordanceData>(renderer, affordanceData));
                affordanceData.AddRenderer(renderer, m_AffordanceMap.bodyVisibilityDefinition);
                renderer.AddMaterial(m_ProxyBackgroundMaterial);
            }

            if (m_ProxyAnimator)
                m_ProxyAnimator.Setup(affordanceDefinitions, m_Affordances);
        }

        void Start()
        {
            if (m_ProxyAnimator)
                this.ConnectInterfaces(m_ProxyAnimator, rayOrigin);

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

            AffordanceVisibilityDefinition bodyVisibility = null;
            foreach (var tuple in m_BodyData)
            {
                if (tuple.secondElement.GetVisibility())
                {
                    bodyVisibility = m_AffordanceMap.bodyVisibilityDefinition;
                    break;
                }
            }

            var time = Time.time;

            foreach (var kvp in m_AffordanceData)
            {
                kvp.Value.Update(kvp.Key, time, m_FadeInDuration, m_FadeOutDuration, this, bodyVisibility);
            }

            foreach (var tuple in m_BodyData)
            {
                tuple.secondElement.Update(tuple.firstElement, time, m_FadeInDuration, m_FadeOutDuration, this);
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();

            foreach (var kvp in m_AffordanceData)
            {
                kvp.Value.OnDestroy();
            }

            foreach (var tuple in m_BodyData)
            {
                ObjectUtils.Destroy(tuple.firstElement);
            }
        }

        FacingDirection GetFacingDirection(Vector3 cameraPosition)
        {
            var toCamera = Vector3.Normalize(cameraPosition - m_NaturalOrientation.position);

            var xDot = Vector3.Dot(toCamera, m_NaturalOrientation.right) * m_FacingAngleWeights.x;
            var yDot = Vector3.Dot(toCamera, m_NaturalOrientation.up) * m_FacingAngleWeights.y;
            var zDot = Vector3.Dot(toCamera, m_NaturalOrientation.forward) * m_FacingAngleWeights.z;
            m_FacingAngleWeights = Vector3.one;

            if (Mathf.Abs(xDot) > Mathf.Abs(yDot))
            {
                if (Mathf.Abs(zDot) > Mathf.Abs(xDot))
                {
                    m_FacingAngleWeights.z += k_LastFacingAngleWeight;
                    return zDot > 0 ? FacingDirection.Front : FacingDirection.Back;
                }

                m_FacingAngleWeights.x += k_LastFacingAngleWeight;
                return xDot > 0 ? FacingDirection.Right : FacingDirection.Left;
            }

            if (Mathf.Abs(zDot) > Mathf.Abs(yDot))
            {
                m_FacingAngleWeights.z += k_LastFacingAngleWeight;
                return zDot > 0 ? FacingDirection.Front : FacingDirection.Back;
            }

            m_FacingAngleWeights.y += k_LastFacingAngleWeight;
            return yDot > 0 ? FacingDirection.Top : FacingDirection.Bottom;
        }

        void UpdateFacingDirection(FacingDirection direction)
        {
            foreach (var request in m_FeedbackRequests)
            {
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
                foreach (var tuple in m_BodyData)
                {
                    tuple.secondElement.SetVisibility(true, changedRequest.duration);
                }
                return;
            }

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

            var requestKey = GetRequestKey();
            requestKey.UpdateValues(request);
            RequestData data;
            if (!m_RequestData.TryGetValue(requestKey, out data))
            {
                data = new RequestData();
                m_RequestData[requestKey] = data;
            }
            else
            {
                m_RequestKeyPool.Enqueue(requestKey);
            }

            var suppress = data.presentations > request.maxPresentations - 1;
            var suppressPresentation = request.suppressPresentation;
            if (suppressPresentation != null)
                suppress = suppressPresentation();

            if (suppress)
                return;

            foreach (var affordance in m_Affordances)
            {
                if (affordance.control != request.control)
                    continue;

                foreach (var renderer in affordance.renderers)
                {
                    m_AffordanceData[renderer].SetVisibility(!request.suppressExisting, request.duration, changedRequest.control);

                    this.SetHighlight(renderer.gameObject, !request.suppressExisting);

                    var tooltipText = request.tooltipText;
                    if (!string.IsNullOrEmpty(tooltipText) || request.suppressExisting)
                    {
                        foreach (var tooltip in affordance.tooltips)
                        {
                            if (tooltip)
                            {
                                data.visibleThisPresentation = false;
                                tooltip.tooltipText = tooltipText;
                                this.ShowTooltip(tooltip, true, placement: tooltip.GetPlacement(m_FacingDirection),
                                    becameVisible: data.onBecameVisible);
                            }
                        }
                    }
                }
            }
        }

        RequestKey GetRequestKey()
        {
            if (m_RequestKeyPool.Count > 0)
                return m_RequestKeyPool.Dequeue();

            return new RequestKey();
        }

        public void RemoveFeedbackRequest(ProxyFeedbackRequest request)
        {
            foreach (var affordance in m_Affordances)
            {
                if (affordance.control != request.control)
                    continue;

                foreach (var renderer in affordance.renderers)
                {
                    m_AffordanceData[renderer].SetVisibility(false, request.duration, request.control);

                    this.SetHighlight(renderer.gameObject, false);

                    if (!string.IsNullOrEmpty(request.tooltipText))
                    {
                        foreach (var tooltip in affordance.tooltips)
                        {
                            if (tooltip)
                            {
                                tooltip.tooltipText = string.Empty;
                                this.HideTooltip(tooltip, true);
                            }
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

        public SerializedProxyNodeFeedback OnSerializePreferences()
        {
            if (m_RequestData.Count == 0)
                return m_SerializedFeedback;

            if (m_SerializedFeedback == null)
                m_SerializedFeedback = new SerializedProxyNodeFeedback();

            var keys = new List<RequestKey>();
            var values = new List<RequestData>();
            foreach (var kvp in m_RequestData)
            {
                var requestKey = kvp.Key;
                if (!requestKey.HasTooltip())
                    continue;

                keys.Add(requestKey);
                values.Add(kvp.Value);
            }

            m_SerializedFeedback.keys = keys.ToArray();
            m_SerializedFeedback.values = values.ToArray();

            return m_SerializedFeedback;
        }

        public void OnDeserializePreferences(object obj)
        {
            if (obj == null)
                return;

            m_SerializedFeedback = (SerializedProxyNodeFeedback)obj;
            if (m_SerializedFeedback.keys == null)
                return;

            var length = m_SerializedFeedback.keys.Length;
            var keys = m_SerializedFeedback.keys;
            var values = m_SerializedFeedback.values;
            for (var i = 0; i < length; i++)
            {
                m_RequestData[keys[i]] = values[i];
            }
        }
    }
}

