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
		readonly Dictionary<Node, int> m_ActiveFeedbackRequests = new Dictionary<Node, int>();

		protected Dictionary<Node, Transform> m_RayOrigins;

		ProxyHelper m_LeftProxyHelper;
		ProxyHelper m_RightProxyHelper;
		int m_ElementsHighlighted;
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
					leftBodyRenderersVisible = !value; // TODO support checking for existing feedback requests and handling body visibility when hiding/unHiding
					rightBodyRenderersVisible = !value;
				}
			}
		}

		private bool m_Hidden;

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }
		public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

		private bool leftBodyRenderersVisible { set { m_LeftProxyHelper.bodyRenderersVisible = value; } }
		private bool rightBodyRenderersVisible { set { m_RightProxyHelper.bodyRenderersVisible = value; } }

		public virtual void Awake()
		{
			m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;

			m_LeftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			m_RightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

			m_ProxyMeshRoots.Add(m_LeftProxyHelper.meshRoot);
			m_ProxyMeshRoots.Add(m_RightProxyHelper.meshRoot);

			m_ActiveFeedbackRequests.Add(Node.LeftHand, 0);
			m_ActiveFeedbackRequests.Add(Node.RightHand, 0);

			var leftButtons = new Dictionary<VRInputDevice.VRControl, AffordanceObject>();
			foreach (var button in m_LeftProxyHelper.Affordances)
			{
				leftButtons[button.control] = button;
			}
			m_Affordances[Node.LeftHand] = leftButtons;
			//m_LeftProxyHelper.controlToAffordanceMap = leftButtons;

			var rightButtons = new Dictionary<VRInputDevice.VRControl, AffordanceObject>();
			foreach (var button in m_RightProxyHelper.Affordances)
			{
				rightButtons[button.control] = button;
			}
			m_Affordances[Node.RightHand] = rightButtons;
			//m_RightProxyHelper.controlToAffordanceMap = rightButtons;

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

			var m_LeftTooltips = new List<Tooltip>();
			var m_RightTooltips = new List<Tooltip>();

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

				Debug.LogError("FeedbackRequests: <color=yellow>" + m_FeedbackRequests.Count + "</color>");
				Debug.LogError("ACTIVE LEFT FeedbackRequests: <color=green>" + m_ActiveFeedbackRequests[Node.LeftHand] + "</color>");
			}
		}

		public void AddFeedbackRequest(FeedbackRequest request)
		{
			//validate requests for duplicates
			Debug.LogError("RequestAdded");
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
			{
				m_FeedbackRequests.Add(proxyRequest); // TODO delete after dictionary addition?
				ExecuteFeedback(proxyRequest);
			}
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
						var tooltip = button.transform.GetComponent<Tooltip>(); // TODO optimize GetComponent calls on buttons via cacheing
						var tooltipText = request.tooltipText;
						if (!string.IsNullOrEmpty(tooltipText) && tooltip)
						{
							tooltip.tooltipText = tooltipText;
							this.ShowTooltip(tooltip, true, k_FeedbackDuration);

							/*
							var existingFeedback = m_ActiveFeedbackRequests.ContainsKey(tooltip);
							if (existingFeedback)
								m_ActiveFeedbackRequests[tooltip] = request; // Tooltips will hide themselves, replace the last existing reference
							else
								m_ActiveFeedbackRequests.Add(tooltip, request);
							*/
						}
					}

					m_ActiveFeedbackRequests[Node.LeftHand] += 1; // Increase feedback request count for this hand/node
				}
			}

			UpdateBodyTransparency();
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			Debug.LogError("RequestRemoved - PUBLIC");
			m_ActiveFeedbackRequests[Node.LeftHand] -= 1; // Increase feedback request count for this hand/node

			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
				RemoveFeedbackRequest(proxyRequest);

			/*
			var existingFeedback = m_ActiveFeedbackRequests.ContainsValue(proxyRequest);
			if (existingFeedback)
			{
				//m_ActiveFeedbackRequests.Remove()[tooltip] = request; // Tooltips will hide themselves, remove the last existing reference
				var tooltip = m_ActiveFeedbackRequests.FirstOrDefault(x => x.Value == proxyRequest).Key;
				if (tooltip != null)
					m_ActiveFeedbackRequests.Remove(tooltip);
			}

			foreach (var proxyNode in m_Buttons)
			{
				foreach (var kvp in proxyNode.Value)
				{
					var button = kvp.Value;
					foreach (var x in m_ActiveFeedbackRequests.Values)
					{

					}
			}

			var existingFeedback = m_ActiveFeedbackRequests.ContainsKey(tooltip);
			if (existingFeedback)
				m_ActiveFeedbackRequests.Remove(tooltip); // Tooltips will hide themselves, remove the last existing reference
			*/
		}

		void RemoveFeedbackRequest(ProxyFeedbackRequest request)
		{
			Debug.LogError("RequestRemoved - PRIVATE");
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
							//m_ActiveFeedbackRequests.Remove(tooltip);
						}
					}
				}
			}

			//m_ActiveFeedbackRequests[Node.LeftHand] -= 1; // Increase feedback request count for this hand/node
			m_FeedbackRequests.Remove(request);

			ExecuteFeedback(request);

			UpdateBodyTransparency();
		}

		public void ClearFeedbackRequests(IRequestFeedback caller)
		{
			var requests = caller == null
				? new List<ProxyFeedbackRequest>(m_FeedbackRequests)
				: m_FeedbackRequests.Where(feedbackRequest => feedbackRequest.caller == caller).ToList();

			foreach (var feedbackRequest in requests)
			{
				RemoveFeedbackRequest(feedbackRequest);
				//m_ActiveFeedbackRequests.Remove(feedbackRequest.tooltipText);
			}
		}

		void UpdateBodyTransparency()
		{
			//Debug.LogError("<color=green>" + m_FeedbackRequests.Count + "</color>");
			if (m_ActiveFeedbackRequests.Count > 0)
			{
				//var leftProxyRequestsExist = m_ActiveFeedbackRequests.Where(x => x.Value.node == Node.LeftHand).Any();
				//var rightProxyRequestsExist = m_ActiveFeedbackRequests.Where(x => x.Value.node == Node.RightHand).Any();
				var leftProxyRequestsExist = m_FeedbackRequests.Where(x => x.node == Node.LeftHand).Any();
				var rightProxyRequestsExist = m_FeedbackRequests.Where(x => x.node == Node.RightHand).Any();
				leftBodyRenderersVisible = !leftProxyRequestsExist;
				rightBodyRenderersVisible = !rightProxyRequestsExist;
			}
		}
	}
}
#endif
