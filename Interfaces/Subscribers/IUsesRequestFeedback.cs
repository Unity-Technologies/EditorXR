using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to request feedback
    /// </summary>
    public interface IUsesRequestFeedback : IFunctionalitySubscriber<IProvidesRequestFeedback>
    {
    }

    public static class UsesRequestFeedbackMethods
    {
        /// <summary>
        /// Add a feedback request to the system
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="request">The feedback request</param>
        public static void AddFeedbackRequest(this IUsesRequestFeedback user, FeedbackRequest request)
        {
#if !FI_AUTOFILL
            user.provider.AddFeedbackRequest(request);
#endif
        }

        /// <summary>
        /// Remove a feedback request from the system
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="request">The feedback request</param>
        public static void RemoveFeedbackRequest(this IUsesRequestFeedback user, FeedbackRequest request)
        {
#if !FI_AUTOFILL
            user.provider.RemoveFeedbackRequest(request);
#endif
        }

        /// <summary>
        /// Clear all feedback requests submitted by this caller from the system
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="caller">The caller object</param>
        public static void ClearFeedbackRequests(this IUsesRequestFeedback user, IUsesRequestFeedback caller)
        {
#if !FI_AUTOFILL
            user.provider.ClearFeedbackRequests(caller);
#endif
        }

        /// <summary>
        /// Get a pooled FeedbackRequest object from the system
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="type">The desired type of feedback request</param>
        /// <param name="caller">The caller object</param>
        public static FeedbackRequest GetFeedbackRequestObject(this IUsesRequestFeedback user, Type type, IUsesRequestFeedback caller)
        {
#if FI_AUTOFILL
            return default(FeedbackRequest);
#else
            return user.provider.GetFeedbackRequestObject(type, caller);
#endif
        }
    }
}
