#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	abstract class TwoHandedProxyBase : MonoBehaviour, IProxy
	{
		const int k_RendererQueue = 9000;

		[SerializeField]
		protected GameObject m_LeftHandProxyPrefab;

		[SerializeField]
		protected GameObject m_RightHandProxyPrefab;

		[SerializeField]
		protected PlayerInput m_PlayerInput;

		internal IInputToEvents m_InputToEvents;

		public Transform leftHand
		{
			get { return m_LeftHand; }
		}
		protected Transform m_LeftHand;

		public Transform rightHand
		{
			get { return m_RightHand; }
		}
		protected Transform m_RightHand;
		readonly List<Material> m_Materials = new List<Material>();

		protected Dictionary<Node, Transform> m_RayOrigins;

		List<Transform> m_ProxyMeshRoots = new List<Transform>();

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

		private bool m_Hidden;

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

			fieldGrabOrigins = new Dictionary<Transform, Transform>
			{
				{ leftProxyHelper.rayOrigin, leftProxyHelper.fieldGrabOrigin },
				{ rightProxyHelper.rayOrigin, rightProxyHelper.fieldGrabOrigin }
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
				m_Materials.AddRange(MaterialUtils.CloneMaterials(r));
			}

			// Move controllers up into EVR range, so they render properly over our UI (e.g. manipulators)
			foreach (var m in m_Materials)
			{
				m.renderQueue = k_RendererQueue;
			}
		}

		public virtual void OnDestroy()
		{
			foreach (var m in m_Materials)
				ObjectUtils.Destroy(m);
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
#endif
