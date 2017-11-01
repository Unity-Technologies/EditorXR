#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight
    {
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

        protected Dictionary<Node, Transform> m_RayOrigins;

        bool m_Hidden;
        ProxyHelper m_LeftProxyHelper;
        ProxyHelper m_RightProxyHelper;
        ProxyUI m_LeftProxyUI;
        ProxyUI m_RightProxyUI;
        List<Transform> m_ProxyMeshRoots = new List<Transform>();

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

                    if (m_LeftProxyUI && m_RightProxyUI)
                    {
                        m_LeftProxyUI.UpdateVisibility();
                        m_RightProxyUI.UpdateVisibility();
                    }
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

            //m_Affordances[Node.LeftHand] = GetAffordanceDictionary(m_LeftProxyHelper);
            //m_Affordances[Node.RightHand] = GetAffordanceDictionary(m_RightProxyHelper);

            m_LeftProxyUI = m_LeftHand.GetComponent<ProxyUI>();
            m_RightProxyUI = m_RightHand.GetComponent<ProxyUI>();

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
            if (m_LeftProxyUI && m_RightProxyUI)
            {
                m_LeftProxyUI.active = active;
                m_RightProxyUI.active = active;
            }

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

                if (Mathf.Max(m_LeftShakeTracker.shakeStrength, m_RightShakeTracker.shakeStrength) > m_ShakeThreshhold)
                {
                    m_LeftProxyUI.AddShakeRequest();
                    m_RightProxyUI.AddShakeRequest();
                }
            }
        }

        public void AddFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null && m_LeftProxyUI && m_RightProxyUI)
            {
                if (proxyRequest.node == Node.LeftHand)
                    m_LeftProxyUI.AddFeedbackRequest(proxyRequest);
                else if (proxyRequest.node == Node.RightHand)
                    m_RightProxyUI.AddFeedbackRequest(proxyRequest);
            }
        }

        public void RemoveFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null && m_LeftProxyUI && m_RightProxyUI)
            {
                if (proxyRequest.node == Node.LeftHand)
                    m_LeftProxyUI.RemoveFeedbackRequest(proxyRequest);
                else if (proxyRequest.node == Node.RightHand)
                    m_RightProxyUI.RemoveFeedbackRequest(proxyRequest);

                //RemoveFeedbackRequest(proxyRequest);
            }
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            // Clear requests for each hand's ProxyUI, in order to prevent out-of-sync errors when exiting EXR
            if (m_LeftProxyUI && m_RightProxyUI)
            {
                m_LeftProxyUI.ClearFeedbackRequests(caller);
                m_RightProxyUI.ClearFeedbackRequests(caller);
            }
        }
    }
}
#endif
