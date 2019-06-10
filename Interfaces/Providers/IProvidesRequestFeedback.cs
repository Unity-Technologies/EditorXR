using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to request feedback
    /// </summary>
    public interface IProvidesRequestFeedback : IFunctionalityProvider
    {
        /// <summary>
        /// Add a feedback request to the system
        /// </summary>
        /// <param name="request">The feedback request</param>
        void AddFeedbackRequest(FeedbackRequest request);

        /// <summary>
        /// Remove a feedback request from the system
        /// </summary>
        /// <param name="request">The feedback request</param>
        void RemoveFeedbackRequest(FeedbackRequest request);

        /// <summary>
        /// Clear all feedback requests submitted by this caller from the system
        /// </summary>
        /// <param name="caller">The caller object</param>
        void ClearFeedbackRequests(IUsesRequestFeedback caller);

        /// <summary>
        /// Get a pooled FeedbackRequest object from the system
        /// </summary>
        /// <param name="type">The desired type of feedback request</param>
        /// <param name="caller">The caller object</param>
        FeedbackRequest GetFeedbackRequestObject(Type type, IUsesRequestFeedback caller);
    }
}
