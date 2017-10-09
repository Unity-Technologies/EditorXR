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
		public bool hideExisting;
	}

	abstract class TwoHandedProxyBase : MonoBehaviour, IProxy, IFeedbackReceiver, ISetTooltipVisibility, ISetHighlight, IConnectInterfaces
	{
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
		readonly List<ProxyFeedbackRequest> m_FeedbackRequests = new List<ProxyFeedbackRequest>();

		protected Dictionary<Node, Transform> m_RayOrigins;

		bool m_Hidden;
		ProxyHelper m_LeftProxyHelper;
		ProxyHelper m_RightProxyHelper;
		List<Transform> m_ProxyMeshRoots = new List<Transform>();

		readonly Dictionary<Node, Dictionary<VRInputDevice.VRControl, List<AffordanceObject>>> m_Affordances =
			new Dictionary<Node, Dictionary<VRInputDevice.VRControl, List<AffordanceObject>>>();

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

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }
		public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

		bool leftAffordanceRenderersVisible { set { m_LeftProxyHelper.affordanceRenderersVisible = value; } }
		bool rightAffordanceRenderersVisible { set { m_RightProxyHelper.affordanceRenderersVisible = value; } }
		bool leftBodyRenderersVisible { set { m_LeftProxyHelper.bodyRenderersVisible = value; } }
		bool rightBodyRenderersVisible { set { m_RightProxyHelper.bodyRenderersVisible = value; } }

		// Local method use only -- created here to reduce garbage collection
		static readonly List<Tooltip> k_TooltipList = new List<Tooltip>();

		public virtual void Awake()
		{
			m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;

			m_LeftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			m_RightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();
			foreach (var tooltip in m_LeftProxyHelper.rightTooltips)
			{
				ObjectUtils.Destroy(tooltip);
			}

			foreach (var tooltip in m_RightProxyHelper.leftTooltips)
			{
				ObjectUtils.Destroy(tooltip);
			}

			m_ProxyMeshRoots.Add(m_LeftProxyHelper.meshRoot);
			m_ProxyMeshRoots.Add(m_RightProxyHelper.meshRoot);

			var leftButtons = new Dictionary<VRInputDevice.VRControl, List<AffordanceObject>>();
			foreach (var button in m_LeftProxyHelper.affordances)
			{
				List<AffordanceObject> buttons;
				if (!leftButtons.TryGetValue(button.control, out buttons))
				{
					buttons = new List<AffordanceObject>();
					leftButtons[button.control] = buttons;
				}

				buttons.Add(button);
			}
			m_Affordances[Node.LeftHand] = leftButtons;

			var rightButtons = new Dictionary<VRInputDevice.VRControl, List<AffordanceObject>>();
			foreach (var button in m_RightProxyHelper.affordances)
			{
				List<AffordanceObject> buttons;
				if (!rightButtons.TryGetValue(button.control, out buttons))
				{
					buttons = new List<AffordanceObject>();
					rightButtons[button.control] = buttons;
				}

				buttons.Add(button);
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
			while (!active)
				yield return null;

			// In standalone play-mode usage, attempt to get the TrackedObjectInput
			if (trackedObjectInput == null && m_PlayerInput)
				trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

			var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();
			this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(leftProxyHelper.gameObject), leftProxyHelper.rayOrigin);
			this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(rightProxyHelper.gameObject), rightProxyHelper.rayOrigin);
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
		}

		void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
		{
			if (!active)
				return;

			foreach (var proxyNode in m_Affordances)
			{
				if (proxyNode.Key != changedRequest.node)
					continue;

				foreach (var kvp in proxyNode.Value)
				{
					if (kvp.Key != changedRequest.control)
						continue;

					ProxyFeedbackRequest request = null;
					foreach (var req in m_FeedbackRequests)
					{
						if (req.node != proxyNode.Key || req.control != kvp.Key)
							continue;

						if (request == null || req.priority >= request.priority)
							request = req;
					}

					if (request == null)
						continue;

					foreach (var button in kvp.Value)
					{
						if (button.renderer)
							this.SetHighlight(button.renderer.gameObject, !request.hideExisting, duration: k_FeedbackDuration);

						if (button.transform)
						{
							var tooltipText = request.tooltipText;
							if (!string.IsNullOrEmpty(tooltipText) || request.hideExisting)
							{
								k_TooltipList.Clear();
								button.transform.GetComponents(k_TooltipList);
								foreach (var tooltip in k_TooltipList)
								{
									tooltip.tooltipText = tooltipText;
									this.ShowTooltip(tooltip, true, k_FeedbackDuration);
								}
							}
						}
					}
				}
			}

			UpdateVisibility();
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
				RemoveFeedbackRequest(proxyRequest);
		}

		void RemoveFeedbackRequest(ProxyFeedbackRequest request)
		{
			Dictionary<VRInputDevice.VRControl, List<AffordanceObject>> affordanceDictionary;
			if (m_Affordances.TryGetValue(request.node, out affordanceDictionary))
			{
				List<AffordanceObject> affordances;
				if (affordanceDictionary.TryGetValue(request.control, out affordances))
				{
					foreach(var kvp in affordanceDictionary)
					{
						foreach (var affordance in kvp.Value)
						{
							if (affordance.renderer)
								this.SetHighlight(affordance.renderer.gameObject, false);


							if (affordance.transform)
							{
								k_TooltipList.Clear();
								affordance.transform.GetComponents(k_TooltipList);
								foreach (var tooltip in k_TooltipList)
								{
									tooltip.tooltipText = string.Empty;
									this.HideTooltip(tooltip, true);
								}
							}
						}
					}
				}
			}

			m_FeedbackRequests.Remove(request);

			if (m_FeedbackRequests.Remove(request))
				ExecuteFeedback(request);
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
