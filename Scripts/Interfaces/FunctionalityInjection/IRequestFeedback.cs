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
        public static Func<Type, FeedbackRequest> getFeedbackRequestObject { private get; set; }

        /// <summary>
        /// Add a feedback request to the system
        /// </summary>
        /// <param name="obj">The caller object</param>
        /// <param name="request">The feedback request</param>
        public static void AddFeedbackRequest(this IRequestFeedback obj, FeedbackRequest request)
        {
            request.caller = obj;
            addFeedbackRequest(request);
        }

        /// <summary>
        /// Remove a feedback request from the system
        /// </summary>
        /// <param name="obj">The caller object</param>
        /// <param name="request">The feedback request</param>
        public static void RemoveFeedbackRequest(this IRequestFeedback obj, FeedbackRequest request)
        {
            request.caller = obj;
            removeFeedbackRequest(request);
        }

        /// <summary>
        /// Clear all feedback requests submitted by this caller from the system
        /// </summary>
        /// <param name="obj">The caller object</param>
        public static void ClearFeedbackRequests(this IRequestFeedback obj)
        {
            clearFeedbackRequests(obj);
        }

        /// <summary>
        /// Get a pooled FeedbackRequest object from the system
        /// </summary>
        /// <param name="type">The type of request object desired</param>
        public static FeedbackRequest GetFeedbackRequestObject(this IRequestFeedback obj, Type type)
        {
            return getFeedbackRequestObject(type);
        }
    }
}
