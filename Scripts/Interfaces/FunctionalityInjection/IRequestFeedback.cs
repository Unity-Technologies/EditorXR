using System;

namespace UnityEditor.Experimental.EditorVR
{
	public interface IRequestFeedback
	{
	}

	public static class IRequestFeedbackMethods
	{
		public static Action<FeedbackRequest> addFeedbackRequest { private get; set; }
		public static Action<FeedbackRequest> removeFeedbackRequest { private get; set; }
		public static Action<IRequestFeedback> clearFeedbackRequests { private get; set; }

		public static void AddFeedbackRequest<TFeedbackRequest>(this IRequestFeedback obj, TFeedbackRequest request) where TFeedbackRequest : FeedbackRequest
		{
			request.caller = obj;
			addFeedbackRequest(request);
		}

		public static void RemoveFeedbackRequest<TFeedbackRequest>(this IRequestFeedback obj, TFeedbackRequest request) where TFeedbackRequest : FeedbackRequest
		{
			request.caller = obj;
			removeFeedbackRequest(request);
		}

		public static void ClearFeedbackRequests(this IRequestFeedback obj)
		{
			clearFeedbackRequests(obj);
		}
	}
}
