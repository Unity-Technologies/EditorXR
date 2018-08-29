#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to be automatically positioned by the AdaptivePositionModule
    /// </summary>
    public interface IAdaptPosition
    {
        /// <summary>
        /// Bool denoting that this implementer is active, and will have it's position adjusted automatically
        /// </summary>
        bool allowAdaptivePositioning { get; }

        /// <summary>
        /// Transform utilized by the AdaptivePositionModule to reposition this transform when the allowedGazeDivergence threshold is surpassed.
        /// If null, the adaptivePositionModule will not manually re-position the implementor, regardless of applicable criteria
        /// </summary>
        Transform adaptiveTransform { get; }

        /// <summary>
        /// Bool denoting that this implementer is being moved by the AdaptivePositionModule
        /// </summary>
        bool beingMoved { set; }

        /// <summary>
        /// Bool denoting that this implementer is within the allowed gaze range, & being looked at
        /// </summary>
        bool inFocus { get; set; }

        /// <summary>
        /// Angle representing the allowed amount of tolerance between the gaze's forward vector & the implementer transform,
        /// beyond which the implementer will be repositioned by the AdaptivePositionModule
        /// </summary>
        float allowedDegreeOfGazeDivergence { get; }

        /// <summary>
        /// Target z-offset, at which to position the gaze source transform
        /// </summary>
        float distanceOffset { get; }

        /// <summary>
        /// The data defining the adaptive position state of the implementer
        /// </summary>
        AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }

        /// <summary>
        /// Bool denoting that this implementer should have its position immediately reset when the next scheduled position update occurs
        /// </summary>
        bool resetAdaptivePosition { get; set; }
    }
}
#endif
