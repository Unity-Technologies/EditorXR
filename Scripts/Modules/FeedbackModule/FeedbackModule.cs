#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public abstract class FeedbackRequest
	{
		public IRequestFeedback caller;
	}

	public class FeedbackModule : MonoBehaviour, IInterfaceConnector
	{
		readonly List<IFeedbackReciever> m_FeedbackReceivers = new List<IFeedbackReciever>();

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

		void AddFeedbackRequest(FeedbackRequest request)
		{
			foreach (var receiver in m_FeedbackReceivers)
			{
				receiver.AddFeedbackRequest(request);
			}
		}

		public void RemoveFeedbackRequest(FeedbackRequest request)
		{
			//Debug.Log("add");
			foreach (var receiver in m_FeedbackReceivers)
			{
				receiver.RemoveFeedbackRequest(request);
			}
		}

		public void ClearFeedbackRequests(IRequestFeedback caller)
		{
			foreach (var reciever in m_FeedbackReceivers)
			{
				reciever.ClearFeedbackRequests(caller);
			}
		}
	}
}
#endif
