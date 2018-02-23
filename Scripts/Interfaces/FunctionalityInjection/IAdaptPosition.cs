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
        /// Transform utilized by the AdaptivePositionModule to reposition this transform when the allowedGazeDivergence threshold is surpassed.
        /// If null, the adaptivePositionModule will not manually re-position the implementor, regardless of applicable criteria
        /// </summary>
        Transform adaptiveTransform { get; }

        /// <summary>
        /// Bool denoting that this implementor is being moved by the AdaptivePositionModule
        /// </summary>
        bool beingMoved { get; set; }

        /// <summary>
        /// Dot-product representing the allowed amount of tolerance between the gaze and the implementor transform,
        /// beyond which the implementor will be repositioned by the AdaptivePositionModule
        /// </summary>
        float allowedGazeDivergence { get; }

        /// <summary>
        /// Target z-offset, at which to position the gaze source transform
        /// </summary>
        float m_DistanceOffset { get; }

        /// <summary>
        /// The data defining the adaptive position state of the implementer
        /// </summary>
        AdaptivePositionModule.AdaptivePositionData adaptivePositionData { get; set; }
    }
}
#endif
