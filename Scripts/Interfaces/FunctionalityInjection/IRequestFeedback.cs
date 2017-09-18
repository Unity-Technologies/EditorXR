using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IRequestFeedback
	{
	}

	public static class IRequestFeedbackMethods
	{
		public delegate void AddFeedbackRequestDelegate(FeedbackRequest request, object caller, int priority = 0);
		public delegate void RemoveFeedbackRequestDelegate(FeedbackRequest request);

		public static AddFeedbackRequestDelegate addFeedbackRequest { private get; set; }
		public static RemoveFeedbackRequestDelegate removeFeedbackRequest { private get; set; }
		public static Action<object> clearFeedbackRequests { private get; set; }

		public static void AddFeedbackRequest<TFeedbackRequest>(this IRequestFeedback obj, TFeedbackRequest request, object caller, int priority = 0) where TFeedbackRequest : FeedbackRequest
		{
			addFeedbackRequest(request, caller, priority);
		}

		public static void RemoveFeedbackRequest<TFeedbackRequest>(this IRequestFeedback obj, TFeedbackRequest request) where TFeedbackRequest : FeedbackRequest
		{
			removeFeedbackRequest(request);
		}

		public static void ClearFeedbackRequests(this IRequestFeedback obj, Type feedbackType, object caller)
		{
			clearFeedbackRequests(caller);
		}
	}
}
