namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Base class for feedback requests
    /// </summary>
    public abstract class FeedbackRequest
    {
        /// <summary>
        /// The calling object
        /// </summary>
        public IUsesRequestFeedback caller { get; set; }

        /// <summary>
        /// Reset the state of this request for re-use
        /// </summary>
        public abstract void Reset();
    }
}
