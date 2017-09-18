#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
		readonly List<IFeedbackReciever>  m_FeedbackReceivers = new List<IFeedbackReciever>();

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
				m_FeedbackReceivers.Add(feedbackReceiver);
		}

		public void DisconnectInterface(object obj, Transform rayOrigin = null)
		{
			var feedbackReceiver = obj as IFeedbackReciever;
			if (feedbackReceiver != null)
				m_FeedbackReceivers.Remove(feedbackReceiver);
		}

		public void AddFeedbackRequest<TFeedbackRequest>(TFeedbackRequest request, object caller, int priority = 0) where TFeedbackRequest : FeedbackRequest
		{
			Debug.Log("asdf");
			foreach (var obj in m_FeedbackReceivers)
			{
				Debug.Log(obj.GetType() + ", " + typeof(TFeedbackRequest) + ", " + typeof(IFeedbackReciever<TFeedbackRequest>).IsAssignableFrom(obj.GetType()));
				var reciever = obj as IFeedbackReciever<TFeedbackRequest>;
				Debug.Log(reciever);
				if (reciever == null)
					continue;

				Debug.Log(obj);

				request.caller = caller;
				request.priority = priority;
				reciever.AddFeedbackRequest(request);
			}
		}

		public void RemoveFeedbackRequest<TFeedbackRequest>(TFeedbackRequest request) where TFeedbackRequest : FeedbackRequest
		{
			foreach (var obj in m_FeedbackReceivers)
			{
				var reciever = obj as IFeedbackReciever<TFeedbackRequest>;
				if (reciever == null)
					continue;

				reciever.RemoveFeedbackRequest(request);
			}
		}

		public void ClearFeedbackRequests(object caller)
		{
			foreach (var obj in m_FeedbackReceivers)
			{
				obj.ClearFeedbackRequests(caller);
			}
		}
	}
}
#endif
