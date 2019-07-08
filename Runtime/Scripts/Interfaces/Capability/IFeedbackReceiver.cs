using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Implementors can receive Feedback Requests
    /// </summary>
    public interface IFeedbackReceiver
    {
        /// <summary>
        /// Add a feedback request to be presented by this receiver
        /// </summary>
        /// <param name="request">Information about the request, usually cast to a custom type defined by the receiver</param>
        void AddFeedbackRequest(FeedbackRequest request);

        /// <summary>
        /// Remove a feedback request and stop presenting it
        /// </summary>
        /// <param name="request">The request object used in AddFeedbackRequest</param>
        void RemoveFeedbackRequest(FeedbackRequest request);

        /// <summary>
        /// Clear feedback requests that were added by this caller.
        /// The FeedbackModule can also call this with a null argument, signaling the intent to clear all requests from all callers.
        /// </summary>
        /// <param name="caller">The IRequestFeedback whose requests will be cleared</param>
        void ClearFeedbackRequests(IRequestFeedback caller);
    }
}
