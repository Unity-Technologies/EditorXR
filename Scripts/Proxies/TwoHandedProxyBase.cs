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
        private readonly string kRayOriginName = "RayOrigin";
        protected readonly string kMenuOriginName = "MenuOrigin";
        protected readonly string MenuInputOriginName = "MenuInputOrigin";
        
        public virtual Dictionary<Node, Transform> rayOrigins
        {
            get { return m_RayOrigins; }
        }

        protected Dictionary<Node, Transform> m_RayOrigins;

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

        public Transform menuInputOrigin { get; set; }
        public Transform menuOrigin { get; set; }

        public virtual void Awake()
        {
            m_LeftHand = U.Object.InstantiateAndSetActive(m_LeftHandProxyPrefab, transform).transform;
            m_RightHand = U.Object.InstantiateAndSetActive(m_RightHandProxyPrefab, transform).transform;
            m_LeftHandRayOrigin = m_LeftHand.FindChild(kRayOriginName);
            m_RightHandRayOrigin = m_RightHand.FindChild(kRayOriginName);

            // The menu target transform should only be on the left hand by default, unless specificed otherwise
            menuOrigin = m_LeftHand.FindChild(kMenuOriginName) ?? m_RightHand.FindChild(kMenuOriginName);
            if (menuOrigin != null)
                menuInputOrigin = m_LeftHand.FindChild(MenuInputOriginName) ?? m_RightHand.FindChild(MenuInputOriginName);
            
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
