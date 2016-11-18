using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.VR.Tools;
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
		
		protected Dictionary<Node, Transform> m_RayOrigins;
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
				if (value != m_Hidden)
				{
					m_Hidden = value;
					m_LeftHand.gameObject.SetActive(!value);
					m_RightHand.gameObject.SetActive(!value);
				}
			}
		}
		private bool m_Hidden;

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }

		public virtual void Awake()
		{
			m_LeftHand = U.Object.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = U.Object.Instantiate(m_RightHandProxyPrefab, transform).transform;
			var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();
			
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
		}

		public virtual void Start()
		{
			// In standalone play-mode usage, attempt to get the TrackedObjectInput 
			if (trackedObjectInput == null && m_PlayerInput)
				trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();
		}

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
	}
}
