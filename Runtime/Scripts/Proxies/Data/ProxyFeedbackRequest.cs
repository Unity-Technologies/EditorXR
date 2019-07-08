using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Proxies
{
    using VRControl = VRInputDevice.VRControl;

    /// <summary>
    /// ProxyFeedbackRequests reside in feedbackRequest collection until the action associated with an affordance changes
    /// Some are removed immediately after being added; others exist for the duration of an action/tool's lifespan
    /// </summary>
    public class ProxyFeedbackRequest : FeedbackRequest
    {
        const float k_DefaultDuration = 5f;
        const int k_DefaultMaxPresentations = 20;

        /// <summary>
        /// The priority level for this request. Default is 0.
        /// If there are multiple requests for one control, the one with the highest priority is presented
        /// If multiple requests on the same control have the same priority, the most recently added is presented
        /// </summary>
        public int priority;

        /// <summary>
        /// The control index on which to present this feedback
        /// </summary>
        public VRControl control;

        /// <summary>
        /// The node on which to present this feedback
        /// </summary>
        public Node node;

        /// <summary>
        /// The text to display in the tooltip that is presented
        /// </summary>
        public string tooltipText;

        /// <summary>
        /// Whether this feedback should suppress (hide) existing feedback on the same control/node
        /// </summary>
        public bool suppressExisting;

        /// <summary>
        /// Whether this feedback should show the body of this node. The priority, control, and tooltipText fields are ignored
        /// </summary>
        public bool showBody;

        /// <summary>
        /// The duration of the presentation
        /// </summary>
        public float duration = k_DefaultDuration;

        /// <summary>
        /// The maximum number times to present this feedback
        /// </summary>
        public int maxPresentations = k_DefaultMaxPresentations;

        /// <summary>
        /// (Optional) A delegate which returns true if presentation should be suppressed
        /// If the delegate is not null, feedback will be suppressed after it becomes visible a number of times (specified by maxPresentations)
        /// </summary>
        public Func<bool> suppressPresentation;

        public override void Reset()
        {
            priority = default(int);
            control = default(VRControl);
            node = default(Node);
            tooltipText = default(string);
            suppressExisting = default(bool);
            showBody = default(bool);
            duration = k_DefaultDuration;
            maxPresentations = k_DefaultMaxPresentations;
        }
    }
}
