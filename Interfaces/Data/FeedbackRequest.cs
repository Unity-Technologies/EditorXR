namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Base class for feedback requests
    /// </summary>
    public abstract class FeedbackRequest
    {
        public IUsesRequestFeedback caller { get; set; }

        public abstract void Reset();
    }
}
