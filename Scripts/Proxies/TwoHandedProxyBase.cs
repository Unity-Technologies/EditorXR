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
		const float k_FeedbackDuration = 500f;

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

		readonly Dictionary<Node, Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>>> m_Buttons =
			new Dictionary<Node, Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>>>();

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
				}
			}
		}

		public Dictionary<Transform, Transform> menuOrigins { get; set; }
		public Dictionary<Transform, Transform> alternateMenuOrigins { get; set; }
		public Dictionary<Transform, Transform> previewOrigins { get; set; }
		public Dictionary<Transform, Transform> fieldGrabOrigins { get; set; }

		// Local method use only -- created here to reduce garbage collection
		static readonly List<Tooltip> k_TooltipList = new List<Tooltip>();

		public virtual void Awake()
		{
			m_LeftHand = ObjectUtils.Instantiate(m_LeftHandProxyPrefab, transform).transform;
			m_RightHand = ObjectUtils.Instantiate(m_RightHandProxyPrefab, transform).transform;
			var leftProxyHelper = m_LeftHand.GetComponent<ProxyHelper>();
			var rightProxyHelper = m_RightHand.GetComponent<ProxyHelper>();

			foreach (var tooltip in leftProxyHelper.rightTooltips)
			{
				ObjectUtils.Destroy(tooltip);
			}

			foreach (var tooltip in rightProxyHelper.leftTooltips)
			{
				ObjectUtils.Destroy(tooltip);
			}

			var leftButtons = new Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>>();
			foreach (var button in leftProxyHelper.buttons)
			{
				List<ProxyHelper.ButtonObject> buttons;
				if (!leftButtons.TryGetValue(button.control, out buttons))
				{
					buttons = new List<ProxyHelper.ButtonObject>();
					leftButtons[button.control] = buttons;
				}

				buttons.Add(button);
			}
			m_Buttons[Node.LeftHand] = leftButtons;

			var rightButtons = new Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>>();
			foreach (var button in rightProxyHelper.buttons)
			{
				List<ProxyHelper.ButtonObject> buttons;
				if (!rightButtons.TryGetValue(button.control, out buttons))
				{
					buttons = new List<ProxyHelper.ButtonObject>();
					rightButtons[button.control] = buttons;
				}

				buttons.Add(button);
			}
			m_Buttons[Node.RightHand] = rightButtons;

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

#pragma warning disable 162
			if (false)
				yield return null; //Unreachable yield to fix compiler error
#pragma warning restore 162
		}

		public virtual void OnDestroy()
		{
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
				m_FeedbackRequests.Add(proxyRequest);
				ExecuteFeedback(proxyRequest);
			}
		}

		void ExecuteFeedback(ProxyFeedbackRequest changedRequest)
		{
			foreach (var proxyNode in m_Buttons)
			{
				foreach (var kvp in proxyNode.Value)
				{
					ProxyFeedbackRequest request = null;
					foreach (var req in m_FeedbackRequests)
					{
						var matchChanged = req.node == changedRequest.node && req.control == changedRequest.control;
						var matchButton = req.node == proxyNode.Key && req.control == kvp.Key;
						var sameCaller = req.caller == changedRequest.caller;
						var priority = request == null || req.priority >= request.priority;
						if (matchButton && priority && (matchChanged || sameCaller))
							request = req;
					}

					if (request == null)
						continue;

					foreach (var button in kvp.Value)
					{
						if (button.renderer)
							this.SetHighlight(button.renderer.gameObject, true, duration: k_FeedbackDuration);

						if (button.transform)
						{
							var tooltipText = request.tooltipText;
							if (!string.IsNullOrEmpty(tooltipText))
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
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			var proxyRequest = request as ProxyFeedbackRequest;
			if (proxyRequest != null)
				RemoveFeedbackRequest(proxyRequest);
		}

		void RemoveFeedbackRequest(ProxyFeedbackRequest request)
		{
			Dictionary<VRInputDevice.VRControl, List<ProxyHelper.ButtonObject>> group;
			if (m_Buttons.TryGetValue(request.node, out group))
			{
				List<ProxyHelper.ButtonObject> buttons;
				if (group.TryGetValue(request.control, out buttons))
				{
					foreach (var button in buttons)
					{
						if (button.renderer)
							this.SetHighlight(button.renderer.gameObject, false);

						if (button.transform)
						{
							k_TooltipList.Clear();
							button.transform.GetComponents(k_TooltipList);
							foreach (var tooltip in k_TooltipList)
							{
								tooltip.tooltipText = string.Empty;
								this.HideTooltip(tooltip, true);
							}
						}
					}
				}
			}
			m_FeedbackRequests.Remove(request);

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
	}
}
#endif
