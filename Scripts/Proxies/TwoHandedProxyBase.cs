#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
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
		public bool visible;
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
		readonly Dictionary<ProxyFeedbackRequest, Coroutine> m_FeedbackRequests = new Dictionary<ProxyFeedbackRequest, Coroutine>();

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

			m_ProxyMeshRoots.Add(m_LeftProxyHelper.meshRoot);
			m_ProxyMeshRoots.Add(m_RightProxyHelper.meshRoot);

			m_Affordances[Node.LeftHand] = GetAffordanceDictionary(m_LeftProxyHelper);
			m_Affordances[Node.RightHand] = GetAffordanceDictionary(m_RightProxyHelper);

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

		static Dictionary<VRInputDevice.VRControl, List<AffordanceObject>> GetAffordanceDictionary(ProxyHelper helper)
		{
			var buttonDictionary = new Dictionary<VRInputDevice.VRControl, List<AffordanceObject>>();
			foreach (var button in helper.affordances)
			{
				List<AffordanceObject> affordances;
				if (!buttonDictionary.TryGetValue(button.control, out affordances))
				{
					affordances = new List<AffordanceObject>();
					buttonDictionary[button.control] = affordances;
				}

				affordances.Add(button);
			}
			return buttonDictionary;
		}

		public virtual IEnumerator Start()
		{
			while (!active)
				yield return null;

			// In standalone play-mode usage, attempt to get the TrackedObjectInput
			if (trackedObjectInput == null && m_PlayerInput)
				trackedObjectInput = m_PlayerInput.GetActions<TrackedObject>();

			this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(m_LeftProxyHelper.gameObject), m_LeftProxyHelper.rayOrigin);
			this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(m_RightProxyHelper.gameObject), m_RightProxyHelper.rayOrigin);
			this.ConnectInterfaces(ObjectUtils.AddComponent<ProxyAnimator>(m_RightProxyHelper.gameObject), m_RightProxyHelper.rayOrigin);
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

		public void AddFeedbackRequest(FeedbackRequest request)
		{
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
			{
				var hasKey = m_FeedbackRequests.ContainsKey(proxyRequest);
				if (hasKey) // Update existing request/coroutine pair
				{
					var lifespanMonitoringCoroutine = m_FeedbackRequests[proxyRequest];
					this.RestartCoroutine(ref lifespanMonitoringCoroutine, MonitorFeedbackRequestLifespan(proxyRequest));
					m_FeedbackRequests[proxyRequest] = lifespanMonitoringCoroutine;
				}
				else // Add a new request/coroutine pair
				{
					var newMonitoringCoroutine = StartCoroutine(MonitorFeedbackRequestLifespan(proxyRequest));
					m_FeedbackRequests.Add(proxyRequest, newMonitoringCoroutine);
				}

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
						var key = req.Key;
						if (key.node != proxyNode.Key || key.control != kvp.Key)
							continue;

						if (request == null || key.priority >= request.priority)
							request = key;
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
			foreach (var kvp in m_FeedbackRequests)
			{
				if (kvp.Key.caller == caller)
					RemoveFeedbackRequest(kvp.Key);
			}
		}

		void UpdateVisibility()
		{
			var rightProxyRequestsExist = false;
			var leftProxyRequestsExist = false;
			if (m_FeedbackRequests.Count > 0)
			{
				// Find any visible feedback requests for each hand
				rightProxyRequestsExist = m_FeedbackRequests.Any(x => x.Key.node == Node.RightHand && x.Key.visible);
				leftProxyRequestsExist = m_FeedbackRequests.Any(x => x.Key.node == Node.LeftHand && x.Key.visible);
			}

			rightAffordanceRenderersVisible = rightProxyRequestsExist;
			rightBodyRenderersVisible = rightProxyRequestsExist;

			leftAffordanceRenderersVisible = leftProxyRequestsExist;
			leftBodyRenderersVisible = leftProxyRequestsExist;
		}

		IEnumerator MonitorFeedbackRequestLifespan(ProxyFeedbackRequest request)
		{
			request.visible = true;
			var currentDuration = 0f;
			while (request != null && currentDuration < k_FeedbackDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				yield return null;
			}

			if (request != null)
				request.visible = false;

			UpdateVisibility();
		}
	}
}
#endif
