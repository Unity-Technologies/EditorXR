using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Proxies
{
	public abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
	{
		const int kRendererQueue = 9000;

		[SerializeField]
		protected GameObject m_LeftHandProxyPrefab;

		[SerializeField]
		protected GameObject m_RightHandProxyPrefab;

		[SerializeField]
		protected PlayerInput m_PlayerInput;

		protected Transform m_LeftHand;
		protected Transform m_RightHand;
		readonly List<Material> m_Materials = new List<Material>();

		protected Dictionary<Node, Transform> m_RayOrigins;

		List<Transform> m_ProxyMeshRoots = new List<Transform>();

		public virtual Dictionary<Node, Transform> rayOrigins { get { return m_RayOrigins; } }

		public virtual TrackedObject trackedObjectInput { protected get; set; }

		public virtual bool active { get { return true; } }

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

			m_ProxyMeshRoots.Add(leftProxyHelper.meshRoot);
			m_ProxyMeshRoots.Add(rightProxyHelper.meshRoot);

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

		public virtual IEnumerator Start()
		{
			// In standalone play-mode usage, attempt to get the TrackedObjectInput 
			if (trackedObjectInput == null && m_PlayerInput)
				trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

			List<Renderer> renderers = new List<Renderer>();
			while (renderers.Count == 0)
			{
				yield return null;
				foreach (var meshRoot in m_ProxyMeshRoots)
				{
					// Only add models of the device and not anything else that is spawned underneath the hand (e.g. menu button, cone/ray)
					renderers.AddRange(meshRoot.GetComponentsInChildren<Renderer>());
				}
			}

			foreach (var r in renderers)
			{
				m_Materials.AddRange(U.Material.CloneMaterials(r));
			}

			// Move controllers up into EVR range, so they render properly over our UI (e.g. manipulators)
			foreach (var m in m_Materials)
			{
				m.renderQueue = kRendererQueue;
			}
		}

		public virtual void OnDestroy()
		{
			foreach (var m in m_Materials)
				U.Object.Destroy(m);
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
