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
        /// <typeparam name="TRequest">The desired type of feedback request</typeparam>
        /// <param name="caller">The caller object</param>
        /// <returns>A feedback request object in its default initial state</returns>
        TRequest GetFeedbackRequestObject<TRequest>(IUsesRequestFeedback caller) where TRequest : FeedbackRequest, new();
    }
}
