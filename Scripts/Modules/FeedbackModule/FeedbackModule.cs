#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public abstract class FeedbackRequest
	{
		internal object caller;
		internal int priority;
	}

	public class FeedbackModule : MonoBehaviour, IInterfaceConnector
	{
		readonly Dictionary<Type, List<IFeedbackReciever>> m_FeedbackReceivers = new Dictionary<Type, List<IFeedbackReciever>>();

		void Awake()
		{
			IRequestFeedbackMethods.addFeedbackRequest = AddFeedbackRequest;
			IRequestFeedbackMethods.removeFeedbackRequest = RemoveFeedbackRequest;
			IRequestFeedbackMethods.clearFeedbackRequests = ClearFeedbackRequests;
		}

		public void ConnectInterface(object obj, Transform rayOrigin = null)
		{
			var feedbackReceiver = obj as IFeedbackReciever;
			if (feedbackReceiver != null)
			{
				var requestType = obj.GetType().GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFeedbackReciever<>))
					.SelectMany(i => i.GetGenericArguments())
					.First();
				if (requestType != null)
				{
					List<IFeedbackReciever> recievers;
					if (!m_FeedbackReceivers.TryGetValue(requestType, out recievers))
					{
						recievers = new List<IFeedbackReciever>();
						m_FeedbackReceivers[requestType] = recievers;
					}

					recievers.Add(feedbackReceiver);
				}
			}
		}

		public void DisconnectInterface(object obj, Transform rayOrigin = null)
		{
			var feedbackReceiver = obj as IFeedbackReciever;
			if (feedbackReceiver != null)
			{
				var requestType = obj.GetType().GetInterfaces()
					.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFeedbackReciever<>))
					.SelectMany(i => i.GetGenericArguments())
					.First();
				if (requestType != null)
				{
					List<IFeedbackReciever> recievers;
					if (m_FeedbackReceivers.TryGetValue(requestType, out recievers))
					{
						recievers.Remove(feedbackReceiver);

						if (recievers.Count == 0)
							m_FeedbackReceivers.Remove(requestType);
					}
				}
			}
		}

		void AddFeedbackRequest(FeedbackRequest request, object caller, int priority = 0)
		{
			var requestType = request.GetType();
			List<IFeedbackReciever> recievers;
			if (m_FeedbackReceivers.TryGetValue(requestType, out recievers))
			{
				foreach (var obj in recievers)
				{
					request.caller = caller;
					request.priority = priority;
					var receiver = (IFeedbackReciever<FeedbackRequest>)obj;
					receiver.AddFeedbackRequest(request);
				}
			}
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			var requestType = request.GetType();
			List<IFeedbackReciever> recievers;
			if (m_FeedbackReceivers.TryGetValue(requestType, out recievers))
			{
				foreach (var obj in recievers)
				{
					var receiver = (IFeedbackReciever<FeedbackRequest>)obj;
					receiver.RemoveFeedbackRequest(request);
				}
			}
		}

		public void ClearFeedbackRequests(object caller)
		{
			foreach (var obj in m_FeedbackReceivers)
			{
				foreach (var reciever in obj.Value)
				{
					reciever.ClearFeedbackRequests(caller);
				}
			}
		}
	}
}
#endif
