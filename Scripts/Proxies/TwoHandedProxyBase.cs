#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
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

    abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight
    {
        const float k_DefaultFeedbackDuration = 5f;

        class AffordanceDictionary : Dictionary<Node, Dictionary<VRInputDevice.VRControl, List<Affordance>>>
        {
        }

        class FeedbackRequests : List<FeedbackRequestAndCoroutineTuple>
        {
        }

        class FeedbackRequestAndCoroutineTuple : Tuple<ProxyFeedbackRequest, Coroutine>
        {
            public FeedbackRequestAndCoroutineTuple(ProxyFeedbackRequest proxyFeedbackRequest, Coroutine coroutine) : base(proxyFeedbackRequest, coroutine)
            {
            }
        }

        [SerializeField]
        protected GameObject m_LeftHandProxyPrefab;

        [SerializeField]
        protected GameObject m_RightHandProxyPrefab;

        [SerializeField]
        protected PlayerInput m_PlayerInput;

        [SerializeField]
        [Tooltip("How much strength the controllers must be shaken with before fading in")]
        protected float m_ShakeThreshhold = 0.5f;

        [SerializeField]
        [Tooltip("Controls the smoothing and how long of a history detection of left controller shake has")]
        protected ShakeVelocityTracker m_LeftShakeTracker = new ShakeVelocityTracker();

        [SerializeField]
        [Tooltip("Controls the smoothing and how long of a history detection of right controller shake has")]
        protected ShakeVelocityTracker m_RightShakeTracker = new ShakeVelocityTracker();

        protected IInputToEvents m_InputToEvents;

        protected Transform m_LeftHand;
        protected Transform m_RightHand;
        readonly FeedbackRequests m_FeedbackRequests = new FeedbackRequests();

        protected Dictionary<Node, Transform> m_RayOrigins;

        bool m_Hidden;
        ProxyHelper m_LeftProxyHelper;
        ProxyHelper m_RightProxyHelper;
        List<Transform> m_ProxyMeshRoots = new List<Transform>();

        ProxyFeedbackRequest m_SemitransparentLockRequest;
        ProxyFeedbackRequest m_ShakeFeedbackRequest;

        readonly AffordanceDictionary m_Affordances = new AffordanceDictionary();

        bool leftAffordanceRenderersVisible { set { m_LeftProxyHelper.affordanceRenderersVisible = value; } }
        bool rightAffordanceRenderersVisible { set { m_RightProxyHelper.affordanceRenderersVisible = value; } }
        bool leftBodyRenderersVisible { set { m_LeftProxyHelper.bodyRenderersVisible = value; } }
        bool rightBodyRenderersVisible { set { m_RightProxyHelper.bodyRenderersVisible = value; } }

        public Transform leftHand { get { return m_LeftHand; } }
        public Transform rightHand { get { return m_RightHand; } }

        public virtual Dictionary<Node, Transform> rayOrigins { get { return m_RayOrigins; } }

        public virtual TrackedObject trackedObjectInput { protected get; set; }

        public bool active { get { return m_InputToEvents.active; } }

        public event Action activeChanged
        {
            add { m_InputToEvents.activeChanged += value; }
            remove { m_InputToEvents.activeChanged -= value; }
        }

        public virtual bool hidden
        {
            set
            {
                if (value != m_Hidden)
                {
                    m_Hidden = value;
                    m_LeftHand.gameObject.SetActive(!value);
                    m_RightHand.gameObject.SetActive(!value);

                    UpdateVisibility();
                }
            }
        }

        public Dictionary<Transform, Transform> menuOrigins { get; set; }
        public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
        public Dictionary<Transform, Transform> previewOrigins { get; set; }
        public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

        public virtual void Awake()
        {
            m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
            m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;

            m_LeftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
            m_RightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

            m_ProxyMeshRoots.Add(m_LeftProxyHelper.meshRoot);
            m_ProxyMeshRoots.Add(m_RightProxyHelper.meshRoot);

            m_Affordances[Node.LeftHand] = GetAffordanceDictionary(m_LeftProxyHelper);
            m_Affordances[Node.RightHand] = GetAffordanceDictionary(m_RightProxyHelper);

            m_RayOrigins = new Dictionary<Node, Transform>
            {
                { Node.LeftHand, m_LeftProxyHelper.rayOrigin },
                { Node.RightHand, m_RightProxyHelper.rayOrigin }
            };

            menuOrigins = new Dictionary<Transform, Transform>()
            {
                { m_LeftProxyHelper.rayOrigin, m_LeftProxyHelper.menuOrigin },
                { m_RightProxyHelper.rayOrigin, m_RightProxyHelper.menuOrigin },
            };

            alternateMenuOrigins = new Dictionary<Transform, Transform>()
            {
                { m_LeftProxyHelper.rayOrigin, m_LeftProxyHelper.alternateMenuOrigin },
                { m_RightProxyHelper.rayOrigin, m_RightProxyHelper.alternateMenuOrigin },
            };

            previewOrigins = new Dictionary<Transform, Transform>
            {
                { m_LeftProxyHelper.rayOrigin, m_LeftProxyHelper.previewOrigin },
                { m_RightProxyHelper.rayOrigin, m_RightProxyHelper.previewOrigin }
            };

            fieldGrabOrigins = new Dictionary<Transform, Transform>
            {
                { m_LeftProxyHelper.rayOrigin, m_LeftProxyHelper.fieldGrabOrigin },
                { m_RightProxyHelper.rayOrigin, m_RightProxyHelper.fieldGrabOrigin }
            };

            m_ShakeFeedbackRequest = new ProxyFeedbackRequest
            {
                control = VRInputDevice.VRControl.LocalPosition,
                node = Node.None,
                tooltipText = null,
                suppressExisting = true,
                proxyShaken = true
            };
        }

        static Dictionary<VRInputDevice.VRControl, List<Affordance>> GetAffordanceDictionary(ProxyHelper helper)
        {
            var buttonDictionary = new Dictionary<VRInputDevice.VRControl, List<Affordance>>();
            foreach (var button in helper.affordances)
            {
                List<Affordance> affordances;
                if (!buttonDictionary.TryGetValue(button.control, out affordances))
                {
                    affordances = new List<Affordance>();
                    buttonDictionary[button.control] = affordances;
                }

                affordances.Add(button);
            }
            return buttonDictionary;
        }

        public virtual IEnumerator Start()
        {
            while (!active)
                yield return null;

            // In standalone play-mode usage, attempt to get the TrackedObjectInput
            if (trackedObjectInput == null && m_PlayerInput)
                trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

            m_LeftShakeTracker.Initialize(trackedObjectInput.leftPosition.vector3);
            m_RightShakeTracker.Initialize(trackedObjectInput.rightPosition.vector3);
        }

        public virtual void Update()
        {
            if (active)
            {
                var leftLocalPosition = trackedObjectInput.leftPosition.vector3;
                m_LeftHand.localPosition = leftLocalPosition;
                m_LeftHand.localRotation = trackedObjectInput.leftRotation.quaternion;

                var rightLocalPosition = trackedObjectInput.rightPosition.vector3;
                m_RightHand.localPosition = rightLocalPosition;
                m_RightHand.localRotation = trackedObjectInput.rightRotation.quaternion;

                m_LeftShakeTracker.Update(leftLocalPosition, Time.deltaTime);
                m_RightShakeTracker.Update(rightLocalPosition, Time.deltaTime);

                if (m_SemitransparentLockRequest == null)
                {
                    if (Mathf.Max(m_LeftShakeTracker.shakeStrength, m_RightShakeTracker.shakeStrength) > m_ShakeThreshhold)
                    {
                        AddFeedbackRequest(m_ShakeFeedbackRequest);
                    }
                }
            }
        }

        public void AddFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
            {
                FeedbackRequestAndCoroutineTuple existingRequestCoroutineTuple = null;
                foreach (var requestCoroutineTuple in m_FeedbackRequests)
                {
                    if (requestCoroutineTuple.firstElement == proxyRequest)
                    {
                        existingRequestCoroutineTuple = requestCoroutineTuple;
                        break;
                    }
                }

                if (existingRequestCoroutineTuple != null) // Update existing request/coroutine pair
                {
                    var lifespanMonitoringCoroutine = existingRequestCoroutineTuple.secondElement;
                    this.RestartCoroutine(ref lifespanMonitoringCoroutine, MonitorFeedbackRequestLifespan(proxyRequest));
                    existingRequestCoroutineTuple.secondElement = lifespanMonitoringCoroutine;
                }
                else // Add a new request/coroutine pair
                {
                    var newMonitoringCoroutine = StartCoroutine(MonitorFeedbackRequestLifespan(proxyRequest));
                    m_FeedbackRequests.Add(new FeedbackRequestAndCoroutineTuple(proxyRequest, newMonitoringCoroutine));
                }

                ExecuteFeedback(proxyRequest);
            }
        }

        void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
        {
            if (!active)
                return;

            foreach (var proxyNode in m_Affordances)
            {
                if (proxyNode.Key != changedRequest.node)
                    continue;

                foreach (var kvp in proxyNode.Value)
                {
                    if (kvp.Key != changedRequest.control)
                        continue;

                    ProxyFeedbackRequest request = null;
                    foreach (var requestCoroutineTuple in m_FeedbackRequests)
                    {
                        var feedbackRequest = requestCoroutineTuple.firstElement;
                        if (feedbackRequest.node != proxyNode.Key || feedbackRequest.control != kvp.Key)
                            continue;

                        if (request == null || feedbackRequest.priority >= request.priority)
                            request = feedbackRequest;
                    }

                    if (request == null)
                        continue;

                    foreach (var button in kvp.Value)
                    {
                        if (button.renderer)
                            this.SetHighlight(button.renderer.gameObject, !request.suppressExisting, duration: k_DefaultFeedbackDuration);

                        var tooltipText = request.tooltipText;
                        if (!string.IsNullOrEmpty(tooltipText) || request.suppressExisting)
                        {
                            foreach (var tooltip in button.tooltips)
                            {
                                if (tooltip)
                                {
                                    tooltip.tooltipText = tooltipText;
                                    this.ShowTooltip(tooltip, true, k_DefaultFeedbackDuration);
                                }
                            }
                        }
                    }
                }
            }

            UpdateVisibility();
        }

        public void RemoveFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
                RemoveFeedbackRequest(proxyRequest);
        }

        void RemoveFeedbackRequest(ProxyFeedbackRequest request)
        {
            Dictionary<VRInputDevice.VRControl, List<Affordance>> affordanceDictionary;
            if (m_Affordances.TryGetValue(request.node, out affordanceDictionary))
            {
                List<Affordance> affordances;
                if (affordanceDictionary.TryGetValue(request.control, out affordances))
                {
                    foreach (var kvp in affordanceDictionary)
                    {
                        foreach (var affordance in kvp.Value)
                        {
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
                    }
                }
            }

            foreach (var requestCoroutineTuple in m_FeedbackRequests)
            {
                if (requestCoroutineTuple.firstElement == request)
                {
                    m_FeedbackRequests.Remove(requestCoroutineTuple);
                    ExecuteFeedback(request);
                    break;
                }
            }
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            // Interate over keys instead of pairs in the dictionary, in order to prevent out-of-sync errors when exiting EXR
            foreach (var requestCoroutineTuple in m_FeedbackRequests)
            {
                var request = requestCoroutineTuple.firstElement;
                if (request != null && request.caller == caller)
                    RemoveFeedbackRequest(request);
            }
        }

        void UpdateVisibility()
        {
            var rightProxyRequestsExist = false;
            var leftProxyRequestsExist = false;
            var shakenVisibility = m_SemitransparentLockRequest != null;
            if (shakenVisibility)
            {
                // Left & right device affordances should be visible when the input-device is shaken
                rightProxyRequestsExist = true;
                leftProxyRequestsExist = true;
            }
            else if (m_FeedbackRequests.Count > 0)
            {
                // Find any visible feedback requests for each hand
                foreach (var requestCoroutineTuple in m_FeedbackRequests)
                {
                    var request = requestCoroutineTuple.firstElement;
                    var node = request.node;
                    var visible = request.visible;

                    if (!leftProxyRequestsExist)
                        leftProxyRequestsExist = node == Node.LeftHand && visible;

                    if (!rightProxyRequestsExist)
                        rightProxyRequestsExist = node == Node.RightHand && visible;

                    if (rightProxyRequestsExist && leftProxyRequestsExist)
                        break;
                }
            }

            rightAffordanceRenderersVisible = rightProxyRequestsExist;
            leftAffordanceRenderersVisible = leftProxyRequestsExist;

            rightBodyRenderersVisible = shakenVisibility;
            leftBodyRenderersVisible = shakenVisibility;
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
            while (request != null && currentDuration < duration)
            {
                currentDuration += Time.unscaledDeltaTime;
                yield return null;
            }

            if (request != null)
                request.visible = false;

            // Unlock shaken body visibility if this was the most recent request the trigger the full body visibility
            if (m_SemitransparentLockRequest == request)
            {
                m_SemitransparentLockRequest = null;
            }

            UpdateVisibility();
        }
    }
}
#endif
