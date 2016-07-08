using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Proxies
{
    public abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
	{
		public virtual TrackedObject TrackedObjectInput { protected get; set; }

		public virtual bool Active
		{
			get
			{
				return true;
			}
		}

		protected Dictionary<Node, Transform> m_RayOrigins;
		public virtual Dictionary<Node, Transform> RayOrigins
		{
			get
			{
				return m_RayOrigins;
			}
		}

		public virtual bool Hidden
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
			if (TrackedObjectInput == null && m_PlayerInput)
				TrackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
		}

		public virtual void Update()
		{
			m_LeftHand.localPosition = TrackedObjectInput.leftPosition.vector3;
			m_LeftHand.localRotation = TrackedObjectInput.leftRotation.quaternion;

			m_RightHand.localPosition = TrackedObjectInput.rightPosition.vector3;
			m_RightHand.localRotation = TrackedObjectInput.rightRotation.quaternion;
		}
	}
}
