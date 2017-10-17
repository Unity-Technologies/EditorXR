#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
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

    abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight,
        IConnectInterfaces, ISerializePreferences
    {
        [Serializable]
        struct RequestKey
        {
            [SerializeField]
            VRInputDevice.VRControl m_Control;

            [SerializeField]
            Node m_Node;

            [SerializeField]
            string m_TooltipText;

            public RequestKey(ProxyFeedbackRequest request)
            {
                m_Control = request.control;
                m_Node = request.node;
                m_TooltipText = request.tooltipText;
            }

            public override int GetHashCode()
            {
                var hashCode = (int)m_Control ^ (int)m_Node;

                if (m_TooltipText != null)
                    hashCode ^= m_TooltipText.GetHashCode();

                return hashCode;
            }

            public override string ToString()
            {
                return m_Control + ", " + m_Node + ", " + m_TooltipText;
            }
        }

        [Serializable]
        class RequestData
        {
            public int presentations;

            public bool visibleThisPresentation { get; set; }
        }

        [Serializable]
        class SerializedFeedback
        {
            public RequestKey[] keys;
            public RequestData[] values;
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

        SerializedFeedback m_SerializedFeedback;
        readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();
        readonly Dictionary<RequestKey, RequestData> m_RequestData = new Dictionary<RequestKey, RequestData>();

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

            if (m_SerializedFeedback == null)
                m_SerializedFeedback = new SerializedFeedback();

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

                    var feedbackKey = new RequestKey(request);
                    RequestData data;
                    if (!m_RequestData.TryGetValue(feedbackKey, out data))
                    {
                        data = new RequestData();
                        m_RequestData[feedbackKey] = data;
                    }

                    if (data.presentations > request.maxPresentations)
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
                                    this.ShowTooltip(tooltip, true, k_FeedbackDuration, () =>
                                    {
                                        if (!data.visibleThisPresentation)
                                            data.presentations++;

                                        data.visibleThisPresentation = true;
                                    });
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

            var feedbackKey = new RequestKey(request);
            RequestData data;
            if (m_RequestData.TryGetValue(feedbackKey, out data))
                data.visibleThisPresentation = false;

            // If this feedback was removed, show any feedback that might have been blocked by it
            if (m_FeedbackRequests.Remove(request))
                ExecuteFeedback(request);
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            var requests = caller == null ? new List<ProxyFeedbackRequest>(m_FeedbackRequests)
                : m_FeedbackRequests.Where(feedbackRequest => feedbackRequest.caller == caller).ToList();

            foreach (var feedbackRequest in requests)
            {
                RemoveFeedbackRequest(feedbackRequest);
            }
        }

        public object OnSerializePreferences()
        {
            if (!active)
                return null;

            if (m_SerializedFeedback != null)
            {
                var count = m_RequestData.Count;
                var keys = new RequestKey[count];
                var values = new RequestData[count];
                count = 0;
                foreach (var kvp in m_RequestData)
                {
                    keys[count] = kvp.Key;
                    values[count] = kvp.Value;
                    count++;
                }

                m_SerializedFeedback.keys = keys;
                m_SerializedFeedback.values = values;
            }

            return m_SerializedFeedback;
        }

        public void OnDeserializePreferences(object obj)
        {
            m_SerializedFeedback = (SerializedFeedback)obj;
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
#endif
