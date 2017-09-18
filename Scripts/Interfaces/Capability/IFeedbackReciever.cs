#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implementors can receive Feedback Requests
	/// </summary>
	public interface IFeedbackReciever
	{
		void ClearFeedbackRequests(object caller);
	}

	/// <inheritdoc />
	/// <summary>
	/// Implementors can receive Feedback Requests that extend IFeedbackRequest
	/// </summary>
	/// <typeparam name="TFeedbackRequest"></typeparam>
	public interface IFeedbackReciever<in TFeedbackRequest> : IFeedbackReciever where TFeedbackRequest : FeedbackRequest
	{
		void AddFeedbackRequest(TFeedbackRequest request);
		void RemoveFeedbackRequest(TFeedbackRequest request);
	}
}
#endif
