#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors can receive Feedback Requests
	/// </summary>
	public interface IFeedbackReciever
	{
		void AddFeedbackRequest(FeedbackRequest request);
		void RemoveFeedbackRequest(FeedbackRequest request);
		void ClearFeedbackRequests(IRequestFeedback caller);
	}
}
#endif
