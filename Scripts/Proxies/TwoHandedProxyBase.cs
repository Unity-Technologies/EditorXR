using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
    public abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
    {
        [SerializeField]
        protected GameObject m_LeftHandProxyPrefab;
        [SerializeField]
        protected GameObject m_RightHandProxyPrefab;
        [SerializeField]
        protected PlayerInput m_PlayerInput;

        protected Transform m_LeftHand;
        protected Transform m_RightHand;
        protected Transform m_LeftHandRayOrigin;
        protected Transform m_RightHandRayOrigin;

		protected readonly string kMenuOriginName = "MenuOrigin";
        protected readonly string MenuInputOriginName = "MenuInputOrigin";

        private readonly string kRayOriginName = "RayOrigin";
        
        public virtual Dictionary<Node, Transform> rayOrigins
        {
            get { return m_RayOrigins; }
        }

        public virtual TrackedObject trackedObjectInput { protected get; set; }

        public virtual bool active
        {
            get
            {
                return true;
            }
        }

        public virtual bool hidden
        {
            set
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    r.enabled = !value;
            }
        }

        public Dictionary<Node, Transform> menuInputOrigins { get; set; }
        public Dictionary<Node, Transform> menuOrigins { get; set; }
        protected Dictionary<Node, Transform> m_RayOrigins;
		
        public virtual void Awake()
        {
            m_LeftHand = U.Object.InstantiateAndSetActive(m_LeftHandProxyPrefab, transform).transform;
            m_RightHand = U.Object.InstantiateAndSetActive(m_RightHandProxyPrefab, transform).transform;
            m_LeftHandRayOrigin = m_LeftHand.FindChild(kRayOriginName);
            m_RightHandRayOrigin = m_RightHand.FindChild(kRayOriginName);

            // The menu target transform should only be on the left hand by default, unless specificed otherwise
            menuOrigins = new Dictionary<Node, Transform>();
            menuInputOrigins = new Dictionary<Node, Transform>();
            var leftHandMenuOrigin = m_LeftHand.FindChild(kMenuOriginName);
            var rightHandMenuOrigin = m_RightHand.FindChild(kMenuOriginName);
            var leftHandMenuInputOrigin = leftHandMenuOrigin != null ? m_LeftHand.FindChild(MenuInputOriginName) : null;
            var rightHandMenuInputOrigin = rightHandMenuOrigin != null ? m_RightHand.FindChild(MenuInputOriginName) : null;
            if (leftHandMenuInputOrigin != null)
            {
                menuOrigins.Add(Node.LeftHand, leftHandMenuOrigin);
                menuInputOrigins.Add(Node.LeftHand, leftHandMenuInputOrigin);
            }

            if (rightHandMenuInputOrigin != null)
            {
                menuOrigins.Add(Node.RightHand, rightHandMenuOrigin);
                menuInputOrigins.Add(Node.RightHand, rightHandMenuInputOrigin);
            }
            
            m_RayOrigins = new Dictionary<Node, Transform>
            {
                { Node.LeftHand, m_LeftHandRayOrigin },
                { Node.RightHand, m_RightHandRayOrigin }
            };
        }

        public virtual void Start()
        {
            // In standalone play-mode usage, attempt to get the TrackedObjectInput 
            if (trackedObjectInput == null && m_PlayerInput)
                trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
        }

        public virtual void Update()
        {
            m_LeftHand.localPosition = trackedObjectInput.leftPosition.vector3;
            m_LeftHand.localRotation = trackedObjectInput.leftRotation.quaternion;

            m_RightHand.localPosition = trackedObjectInput.rightPosition.vector3;
            m_RightHand.localRotation = trackedObjectInput.rightRotation.quaternion;
        }
    }
}
