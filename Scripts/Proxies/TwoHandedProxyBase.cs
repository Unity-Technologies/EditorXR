using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    /// <summary>
    /// Which cardinal direction a proxy node is facing
    /// </summary>
    [Flags]
    public enum FacingDirection
    {
        Front = 1 << 0,
        Back = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Top = 1 << 4,
        Bottom = 1 << 5
    }

    abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight, ISerializePreferences
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
        ProxyNode m_LeftProxyNode;
        ProxyNode m_RightProxyNode;

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
                }
            }
        }

        public Dictionary<Transform, Transform> menuOrigins { get; set; }
        public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
        public Dictionary<Transform, Transform> previewOrigins { get; set; }
        public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

        protected virtual void Awake()
        {
            m_LeftHand = EditorXRUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
            m_RightHand = EditorXRUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;

            m_LeftProxyNode = m_LeftHand.GetComponent<ProxyNode>();
            m_RightProxyNode = m_RightHand.GetComponent<ProxyNode>();

            m_RayOrigins = new Dictionary<Node, Transform>
            {
                { Node.LeftHand, m_LeftProxyNode.rayOrigin },
                { Node.RightHand, m_RightProxyNode.rayOrigin }
            };

            menuOrigins = new Dictionary<Transform, Transform>()
            {
                { m_LeftProxyNode.rayOrigin, m_LeftProxyNode.menuOrigin },
                { m_RightProxyNode.rayOrigin, m_RightProxyNode.menuOrigin },
            };

            alternateMenuOrigins = new Dictionary<Transform, Transform>()
            {
                { m_LeftProxyNode.rayOrigin, m_LeftProxyNode.alternateMenuOrigin },
                { m_RightProxyNode.rayOrigin, m_RightProxyNode.alternateMenuOrigin },
            };

            previewOrigins = new Dictionary<Transform, Transform>
            {
                { m_LeftProxyNode.rayOrigin, m_LeftProxyNode.previewOrigin },
                { m_RightProxyNode.rayOrigin, m_RightProxyNode.previewOrigin }
            };

            fieldGrabOrigins = new Dictionary<Transform, Transform>
            {
                { m_LeftProxyNode.rayOrigin, m_LeftProxyNode.fieldGrabOrigin },
                { m_RightProxyNode.rayOrigin, m_RightProxyNode.fieldGrabOrigin }
            };
        }

        protected virtual IEnumerator Start()
        {
            hidden = true;
            while (!active)
                yield return null;

            // In standalone play-mode usage, attempt to get the TrackedObjectInput
            if (trackedObjectInput == null && m_PlayerInput)
                trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

            m_LeftShakeTracker.Initialize(trackedObjectInput.leftPosition.vector3);
            m_RightShakeTracker.Initialize(trackedObjectInput.rightPosition.vector3);
        }

        protected virtual void Update()
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

                if (Mathf.Max(m_LeftShakeTracker.shakeStrength, m_RightShakeTracker.shakeStrength) > m_ShakeThreshhold)
                {
                    m_LeftProxyNode.AddShakeRequest();
                    m_RightProxyNode.AddShakeRequest();
                }
            }
        }

        public void AddFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
            {
                if (proxyRequest.node == Node.LeftHand)
                    m_LeftProxyNode.AddFeedbackRequest(proxyRequest);
                else if (proxyRequest.node == Node.RightHand)
                    m_RightProxyNode.AddFeedbackRequest(proxyRequest);
            }
        }

        public void RemoveFeedbackRequest(FeedbackRequest request)
        {
            var proxyRequest = request as ProxyFeedbackRequest;
            if (proxyRequest != null)
            {
                if (proxyRequest.node == Node.LeftHand)
                    m_LeftProxyNode.RemoveFeedbackRequest(proxyRequest);
                else if (proxyRequest.node == Node.RightHand)
                    m_RightProxyNode.RemoveFeedbackRequest(proxyRequest);
            }
        }

        public void ClearFeedbackRequests(IRequestFeedback caller)
        {
            // Check for null in order to prevent MissingReferenceException when exiting EXR
            if (m_LeftProxyNode && m_RightProxyNode)
            {
                m_LeftProxyNode.ClearFeedbackRequests(caller);
                m_RightProxyNode.ClearFeedbackRequests(caller);
            }
        }

        public object OnSerializePreferences()
        {
            if (!active)
                return null;

            var leftNode = m_LeftProxyNode.OnSerializePreferences();
            var rightNode = m_RightProxyNode.OnSerializePreferences();

            if (leftNode == null && rightNode == null)
                return null;

            return new SerializedProxyFeedback
            {
                leftNode = leftNode,
                rightNode = rightNode
            };
        }

        public void OnDeserializePreferences(object obj)
        {
            var serializedFeedback = (SerializedProxyFeedback)obj;

            m_LeftProxyNode.OnDeserializePreferences(serializedFeedback.leftNode);
            m_RightProxyNode.OnDeserializePreferences(serializedFeedback.rightNode);
        }
    }
}
