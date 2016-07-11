using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
    public abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
	{
		public virtual TrackedObject trackedObjectInput { protected get; set; }

		public virtual bool active
		{
			get
			{
				return true;
			}
		}

		protected Dictionary<Node, Transform> m_RayOrigins;
		public virtual Dictionary<Node, Transform> rayOrigins
		{
			get
			{
				return m_RayOrigins;
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

		public virtual void Awake()
		{
			m_LeftHand = U.Object.InstantiateAndSetActive(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = U.Object.InstantiateAndSetActive(m_RightHandProxyPrefab, transform).transform;
			m_LeftHandRayOrigin = m_LeftHand.FindChild("RayOrigin");
			m_RightHandRayOrigin = m_RightHand.FindChild("RayOrigin");

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
