#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
	public class ProxyFeedbackRequest : FeedbackRequest
	{
		public int priority;
		public VRInputDevice.VRControl control;
		public Node node;
		public string tooltipText;
	}

	abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight
	{
		const int k_RendererQueue = 9000;
		const float k_FeedbackDuration = 5f;

		[SerializeField]
		protected GameObject m_LeftHandProxyPrefab;

		[SerializeField]
		protected GameObject m_RightHandProxyPrefab;

		[SerializeField]
		protected PlayerInput m_PlayerInput;

		internal IInputToEvents m_InputToEvents;

		protected Transform m_LeftHand;
		protected Transform m_RightHand;
		readonly List<Material> m_Materials = new List<Material>();
		readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();

		protected Dictionary<Node, Transform> m_RayOrigins;

		ProxyHelper m_LeftProxyHelper;
		ProxyHelper m_RightProxyHelper;
		List<Transform> m_ProxyMeshRoots = new List<Transform>();

		readonly Dictionary<Node, Dictionary<VRInputDevice.VRControl, AffordanceObject>> m_Affordances = new Dictionary<Node, Dictionary<VRInputDevice.VRControl, AffordanceObject>>();

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

					UpdateVisibility();
				}
			}
		}

		private bool m_Hidden;

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }
		public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

		bool leftAffordanceRenderersVisible { set { m_LeftProxyHelper.affordanceRenderersVisible = value; } }
		bool rightAffordanceRenderersVisible { set { m_RightProxyHelper.affordanceRenderersVisible = value; } }
		bool leftBodyRenderersVisible { set { m_LeftProxyHelper.bodyRenderersVisible = value; } }
		bool rightBodyRenderersVisible { set { m_RightProxyHelper.bodyRenderersVisible = value; } }

		public virtual void Awake()
		{
			m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;

			m_LeftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			m_RightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

			m_ProxyMeshRoots.Add(m_LeftProxyHelper.meshRoot);
			m_ProxyMeshRoots.Add(m_RightProxyHelper.meshRoot);

			var leftButtons = new Dictionary<VRInputDevice.VRControl, AffordanceObject>();
			foreach (var button in m_LeftProxyHelper.Affordances)
			{
				leftButtons[button.control] = button;
			}
			m_Affordances[Node.LeftHand] = leftButtons;

			var rightButtons = new Dictionary<VRInputDevice.VRControl, AffordanceObject>();
			foreach (var button in m_RightProxyHelper.Affordances)
			{
				rightButtons[button.control] = button;
			}
			m_Affordances[Node.RightHand] = rightButtons;

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
			{
				ObjectUtils.Destroy(m);
			}
		}

		public virtual void Update()
		{
			if (active)
			{
				m_LeftHand.localPosition = trackedObjectInput.leftPosition.vector3;
				m_LeftHand.localRotation = trackedObjectInput.leftRotation.quaternion;

				m_RightHand.localPosition = trackedObjectInput.rightPosition.vector3;
				m_RightHand.localRotation = trackedObjectInput.rightRotation.quaternion;

				Debug.LogError("FeedbackRequests: <color=yellow>" + m_FeedbackRequests.Count + "</color>");
			}
		}

		public void AddFeedbackRequest(FeedbackRequest request)
		{
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
			{
				m_FeedbackRequests.Add(proxyRequest);
				ExecuteFeedback(proxyRequest);
			}

			UpdateVisibility();
		}

		void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
		{
			foreach (var proxyNode in m_Affordances)
			{
				foreach (var kvp in proxyNode.Value)
				{
					ProxyFeedbackRequest request = null;
					foreach (var req in m_FeedbackRequests)
					{
						var node = req.node;
						var matchChanged = node == changedRequest.node && req.control == changedRequest.control;
						var matchButton = node == proxyNode.Key && req.control == kvp.Key;
						var sameCaller = req.caller == changedRequest.caller;
						var priority = request == null || req.priority >= request.priority;
						if (matchButton && priority && (matchChanged || sameCaller))
							request = req;
					}

					if (request == null)
						continue;

					var button = kvp.Value;
					if (button.renderer)
						this.SetHighlight(button.renderer.gameObject, true, duration: k_FeedbackDuration);

					if (button.transform)
					{
						var tooltip = button.transform.GetComponent<Tooltip>();
						var tooltipText = request.tooltipText;
						if (!string.IsNullOrEmpty(tooltipText) && tooltip)
						{
							tooltip.tooltipText = tooltipText;
							this.ShowTooltip(tooltip, true, k_FeedbackDuration);
						}
					}

				}
			}
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
			{
				RemoveFeedbackRequest(proxyRequest);
			}
		}

		void RemoveFeedbackRequest(ProxyFeedbackRequest request)
		{
			Dictionary<VRInputDevice.VRControl, AffordanceObject> buttons;
			if (m_Affordances.TryGetValue(request.node, out buttons))
			{
				AffordanceObject affordance;
				if (buttons.TryGetValue(request.control, out affordance))
				{
					if (affordance.renderer)
						this.SetHighlight(affordance.renderer.gameObject, false);

					if (affordance.transform)
					{
						var tooltip = affordance.transform.GetComponent<Tooltip>();
						if (tooltip)
						{
							tooltip.tooltipText = string.Empty;
							this.HideTooltip(tooltip, true);
						}
					}
				}
			}

			m_FeedbackRequests.Remove(request);

			ExecuteFeedback(request);

			UpdateVisibility();
		}

		public void ClearFeedbackRequests(IRequestFeedback caller)
		{
			var requests = caller == null
				? new List<ProxyFeedbackRequest>(m_FeedbackRequests)
				: m_FeedbackRequests.Where(feedbackRequest => feedbackRequest.caller == caller).ToList();

			foreach (var feedbackRequest in requests)
			{
				RemoveFeedbackRequest(feedbackRequest);
			}
		}

		void UpdateVisibility()
		{
			Debug.LogError("<color=green>" + m_FeedbackRequests.Count + "</color>");
			var rightProxyRequestsExist = false;
			var leftProxyRequestsExist = false;
			if (m_FeedbackRequests.Count > 0)
			{
				rightProxyRequestsExist = m_FeedbackRequests.Where(x => x.node == Node.RightHand).Any();
				leftProxyRequestsExist = m_FeedbackRequests.Where(x => x.node == Node.LeftHand).Any();
			}

			rightAffordanceRenderersVisible = rightProxyRequestsExist;
			rightBodyRenderersVisible = rightProxyRequestsExist;

			leftAffordanceRenderersVisible = leftProxyRequestsExist;
			leftBodyRenderersVisible = leftProxyRequestsExist;
		}
	}
}
#endif
