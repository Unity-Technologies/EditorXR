using System;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Base class for feedback requests
    /// </summary>
    public abstract class FeedbackRequest
    {
        readonly IUsesRequestFeedback m_Caller;

        public IUsesRequestFeedback caller { get { return m_Caller; } }

        public abstract void Reset();

        public FeedbackRequest(IUsesRequestFeedback caller) { m_Caller = caller; }
    }
}
