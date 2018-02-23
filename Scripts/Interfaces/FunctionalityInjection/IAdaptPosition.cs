#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to be automatically positioned by the AdaptivePositionModule
    /// </summary>
    public interface IAdaptPosition
    {
        /// <summary>
        /// Transform utilized by the AdaptivePositionModule to reposition this transform when the allowedGazeDivergence threshold is surpassed.
        /// If null, the adaptivePositionModule will not manually re-position the implementor, regardless of applicable criteria
        /// </summary>
        Transform transform { get; set; }

        /// <summary>
        /// Bool denoting that this implementor is being moved by the AdaptivePositionModule
        /// </summary>
        bool beingMoved { get; set; }

        /// <summary>
        /// Dot-product representing the allowed amount of tolerance between the gaze and the implementor transform,
        /// beyond which the implementor will be repositioned by the AdaptivePositionModule
        /// </summary>
        float allowedGazeDivergence { get; }
    }
}
#endif
