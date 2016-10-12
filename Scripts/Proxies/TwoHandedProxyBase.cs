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
				var renderers = GetComponentsInChildren<Renderer>();
				foreach (var r in renderers)
					r.enabled = !value;
			}
		}

		public Dictionary<Node, Transform> menuOrigins { get; set; }
		public Dictionary<Node, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Node, Transform> previewOrigins { get; set; }

		public virtual void Awake()
		{
			m_LeftHand = U.Object.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = U.Object.Instantiate(m_RightHandProxyPrefab, transform).transform;
			var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

			// The menu target transform should only be on the left hand by default, unless specificed otherwise
			menuOrigins = new Dictionary<Node, Transform>();
			alternateMenuOrigins = new Dictionary<Node, Transform>();
			var leftHandMenuOrigin = leftProxyHelper.menuOrigin;
			var rightHandMenuOrigin = rightProxyHelper.menuOrigin;
			var leftHandAlternateMenu = leftProxyHelper.alternateMenuOrigin;
			var rightHandAlternateMenu = rightProxyHelper.alternateMenuOrigin;

			// MS: Unless I misunderstand, these two if blocks are overridden by the setters below
			if (leftHandAlternateMenu != null)
			{
				menuOrigins.Add(Node.LeftHand, leftHandMenuOrigin);
				alternateMenuOrigins.Add(Node.LeftHand, leftHandAlternateMenu);
			}

			if (rightHandAlternateMenu != null)
			{
				menuOrigins.Add(Node.RightHand, rightHandMenuOrigin);
				alternateMenuOrigins.Add(Node.RightHand, rightHandAlternateMenu);
			}
			
			m_RayOrigins = new Dictionary<Node, Transform>
			{
				{ Node.LeftHand, leftProxyHelper.rayOrigin },
				{ Node.RightHand, rightProxyHelper.rayOrigin }
			};

			menuOrigins = new Dictionary<Node, Transform>()
			{
				{ Node.LeftHand, leftProxyHelper.menuOrigin },
				{ Node.RightHand, rightProxyHelper.menuOrigin },
			};

			alternateMenuOrigins = new Dictionary<Node, Transform>()
			{
				{ Node.LeftHand, leftProxyHelper.alternateMenuOrigin },
				{ Node.RightHand, rightProxyHelper.alternateMenuOrigin },
			};

			previewOrigins = new Dictionary<Node, Transform>
			{
				{ Node.LeftHand, leftProxyHelper.previewOirign },
				{ Node.RightHand, rightProxyHelper.previewOirign }
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
