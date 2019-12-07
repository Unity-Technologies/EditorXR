using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Gives decorated class ability to be automatically positioned by the AdaptivePositionModule
    /// </summary>
    public interface IAdaptPosition
    {
        /// <summary>
        /// Denotes if this implementer is active, and will have it's position adjusted automatically
        /// </summary>
        bool allowAdaptivePositioning { get; }

        /// <summary>
        /// Transform utilized by the AdaptivePositionModule to reposition this transform when the allowedGazeDivergence threshold is surpassed.
        /// If null, the adaptivePositionModule will not manually re-position the implementor, regardless of applicable criteria
        /// </summary>
        Transform adaptiveTransform { get; }

        /// <summary>
        /// Denotes if this implementer is being moved by the AdaptivePositionModule
        /// </summary>
        bool beingMoved { set; }

        /// <summary>
        /// Denotes if this implementer is within the allowed gaze range, and being looked at
        /// </summary>
        bool inFocus { get; set; }

        /// <summary>
        /// Angle representing the allowed amount of tolerance between the gaze's forward vector and the implementer transform,
        /// beyond which the implementer will be repositioned by the AdaptivePositionModule
        /// </summary>
        float allowedDegreeOfGazeDivergence { get; }

        /// <summary>
        /// Target z-offset, at which to position the gaze source transform
        /// </summary>
        float adaptivePositionRestDistance { get; }

        /// <summary>
        /// Bool denoting that this implementer should have its position immediately reset when the next scheduled position update occurs
        /// </summary>
        bool resetAdaptivePosition { get; set; }

        /// <summary>
        /// Distance below which an object will be re-positioned at the ideal distance from the user's gaze/hmd
        /// </summary>
        float allowedMinDistanceDivergence { get; }

        /// <summary>
        /// Distance beyond which an object will be re-positioned at the ideal distance from the user's gaze/hmd
        /// </summary>
        float allowedMaxDistanceDivergence { get; }

        /// <summary>
        /// Coroutine that handles the animated re-positioning of the object
        /// </summary>
        Coroutine adaptiveElementRepositionCoroutine { get; set; }

        /// <summary>
        /// Adjust position only when out of focus/gaze
        /// This allows an implementer to remain stable while the user move towards/away while focusing upon it
        /// </summary>
        bool onlyMoveWhenOutOfFocus { get; }

        /// <summary>
        /// Adjust position, regardless of distance, if out of focus
        /// </summary>
        bool repositionIfOutOfFocus { get; }
    }
}
