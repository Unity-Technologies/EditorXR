#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    using ButtonDictionary = Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>>;

    public class ProxyFeedbackRequest : FeedbackRequest
    {
        public int priority;
        public VRInputDevice.VRControl control;
        public Node node;
        public string tooltipText;
        public bool hideExisting;
        public int maxPresentations = 2;
    }

    abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight, IConnectInterfaces
    {
        struct ProxyFeedbackRequestKey
        {
            readonly object caller;
            readonly VRInputDevice.VRControl control;
            readonly Node node;
            readonly string tooltipText;

            public ProxyFeedbackRequestKey(ProxyFeedbackRequest request)
            {
                caller = request.caller;
                control = request.control;
                node = request.node;
                tooltipText = request.tooltipText;
            }

            public override int GetHashCode()
            {
                return caller.GetHashCode() ^ (int)control ^ (int)node ^ tooltipText.GetHashCode();
            }
        }

        const float k_FeedbackDuration = 5f;

        [SerializeField]
        protected GameObject m_LeftHandProxyPrefab;

        [SerializeField]
        protected GameObject m_RightHandProxyPrefab;

        [SerializeField]
        protected PlayerInput m_PlayerInput;

        protected IInputToEvents m_InputToEvents;

        protected Transform m_LeftHand;
        protected Transform m_RightHand;
        readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();
        readonly Dictionary<ProxyFeedbackRequestKey, int> m_RequestPresentations = new Dictionary<ProxyFeedbackRequestKey, int>();

        protected Dictionary<Node, Transform> m_RayOrigins;

        bool m_Hidden;

        readonly Dictionary<Node, ButtonDictionary> m_Buttons = new Dictionary<Node, ButtonDictionary>();

        public Transform leftHand
        {
            get { return m_LeftHand; }
        }

        public Transform rightHand
        {
            get { return m_RightHand; }
        }

        public virtual Dictionary<Node, Transform> rayOrigins
        {
            get { return m_RayOrigins; }
        }

        public virtual TrackedObject trackedObjectInput { protected get; set; }

        public bool active
        {
            get { return m_InputToEvents.active; }
        }

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
            var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
            var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

            m_Buttons[Node.LeftHand] = GetButtonDictionary(leftProxyHelper);
            m_Buttons[Node.RightHand] = GetButtonDictionary(rightProxyHelper);

            m_RayOrigins = new Dictionary<Node, Transform>
            {
                { Node.LeftHand, leftProxyHelper.rayOrigin },
                { Node.RightHand, rightProxyHelper.rayOrigin }
            };

            menuOrigins = new Dictionary<Transform, Transform>()
            {
                { leftProxyHelper.rayOrigin, leftProxyHelper.menuOrigin },
                { rightProxyHelper.rayOrigin, rightProxyHelper.menuOrigin },
            };

            alternateMenuOrigins = new Dictionary<Transform, Transform>()
            {
                { leftProxyHelper.rayOrigin, leftProxyHelper.alternateMenuOrigin },
                { rightProxyHelper.rayOrigin, rightProxyHelper.alternateMenuOrigin },
            };

            previewOrigins = new Dictionary<Transform, Transform>
            {
                { leftProxyHelper.rayOrigin, leftProxyHelper.previewOrigin },
                { rightProxyHelper.rayOrigin, rightProxyHelper.previewOrigin }
            };

            fieldGrabOrigins = new Dictionary<Transform, Transform>
            {
                { leftProxyHelper.rayOrigin, leftProxyHelper.fieldGrabOrigin },
                { rightProxyHelper.rayOrigin, rightProxyHelper.fieldGrabOrigin }
            };
        }

        static ButtonDictionary GetButtonDictionary(ProxyHelper helper)
        {
            var buttonDictionary = new ButtonDictionary();
            foreach (var button in helper.buttons)
            {
                List<ProxyHelper.ButtonObject> buttons;
                if (!buttonDictionary.TryGetValue(button.control, out buttons))
                {
                    buttons = new List<ProxyHelper.ButtonObject>();
                    buttonDictionary[button.control] = buttons;
                }

                buttons.Add(button);
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

            var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
            var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();
            this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(leftProxyHelper.gameObject), leftProxyHelper.rayOrigin);
            this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(rightProxyHelper.gameObject), rightProxyHelper.rayOrigin);
        }

        public virtual void OnDestroy() { }

        public virtual void Update()
        {
            if (active)
            {
                m_LeftHand.localPosition = trackedObjectInput.leftPosition.vector3;
                m_LeftHand.localRotation = trackedObjectInput.leftRotation.quaternion;

                m_RightHand.localPosition = trackedObjectInput.rightPosition.vector3;
                m_RightHand.localRotation = trackedObjectInput.rightRotation.quaternion;
            }
        }

        public void AddFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
            {
                m_FeedbackRequests.Add(proxyRequest);
                ExecuteFeedback(proxyRequest);

                var feedbackKey = new ProxyFeedbackRequestKey(proxyRequest);
                int presentations;
                if (!m_RequestPresentations.TryGetValue(feedbackKey, out presentations))
                    m_RequestPresentations[feedbackKey] = 0;

                m_RequestPresentations[feedbackKey] = presentations + 1;
            }
        }

        void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
        {
            if (!active)
                return;

            foreach (var proxyNode in m_Buttons)
            {
                if (proxyNode.Key != changedRequest.node)
                    continue;

                foreach (var kvp in proxyNode.Value)
                {
                    if (kvp.Key != changedRequest.control)
                        continue;

                    ProxyFeedbackRequest request = null;
                    foreach (var req in m_FeedbackRequests)
                    {
                        if (req.node != proxyNode.Key || req.control != kvp.Key)
                            continue;

                        if (request == null || req.priority >= request.priority)
                            request = req;
                    }

                    if (request == null)
                        continue;

                    var feedbackKey = new ProxyFeedbackRequestKey(request);
                    int presentations;
                    if (!m_RequestPresentations.TryGetValue(feedbackKey, out presentations))
                        m_RequestPresentations[feedbackKey] = 0;

                    if (presentations > request.maxPresentations)
                        continue;

                    foreach (var button in kvp.Value)
                    {
                        if (button.renderer)
                            this.SetHighlight(button.renderer.gameObject, !request.hideExisting, duration: k_FeedbackDuration);

                        var tooltipText = request.tooltipText;
                        if (!string.IsNullOrEmpty(tooltipText) || request.hideExisting)
                        {
                            foreach (var tooltip in button.tooltips)
                            {
                                if (tooltip)
                                {
                                    tooltip.tooltipText = tooltipText;
                                    this.ShowTooltip(tooltip, true, k_FeedbackDuration);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
                RemoveFeedbackRequest(proxyRequest);
        }

        void RemoveFeedbackRequest(ProxyFeedbackRequest request)
        {
            Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>> group;
            if (m_Buttons.TryGetValue(request.node, out group))
            {
                List<ProxyHelper.ButtonObject> buttons;
                if (group.TryGetValue(request.control, out buttons))
                {
                    foreach (var button in buttons)
                    {
                        if (button.renderer)
                            this.SetHighlight(button.renderer.gameObject, false);

                        foreach (var tooltip in button.tooltips)
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

            if (m_FeedbackRequests.Remove(request))
                ExecuteFeedback(request);
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            var requests = caller == null
                ? new List<ProxyFeedbackRequest>(m_FeedbackRequests)
                : m_FeedbackRequests.Where(feedbackRequest => feedbackRequest.caller == caller).ToList();

            foreach (var feedbackRequest in requests)
            {
                RemoveFeedbackRequest(feedbackRequest);
            }
        }
    }
}
#endif
