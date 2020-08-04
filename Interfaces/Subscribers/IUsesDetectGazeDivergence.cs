using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above a defined threshold, for a given transform's forward vector
    /// </summary>
    public interface IUsesDetectGazeDivergence : IFunctionalitySubscriber<IProvidesDetectGazeDivergence> { }

    /// <summary>
    /// Extension methods for implementors of IUsesDeleteSceneObject
    /// </summary>
    public static class UsesDetectGazeDivergenceMethods
    {
        /// <summary>
        /// Check if gaze direction is above the divergence threshold
        /// </summary>
        /// <param name="transformToTest">The transform representing gaze direction</param>
        /// <param name="divergenceThreshold">The threshold angle value to test</param>
        /// <param name="disregardTemporalStability">Whether to disregard temporal stability</param>
        /// <returns>True if the angle between the gaze and target is above the divergence threshold</returns>
        public static bool IsAboveDivergenceThreshold(this IUsesDetectGazeDivergence user, Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsAboveDivergenceThreshold(transformToTest, divergenceThreshold, disregardTemporalStability);
#endif
        }

        /// <summary>
        /// Set the divergence recovery speed
        /// </summary>
        /// <param name="rateAtWhichGazeVelocityReturnsToStableThreshold">The rate at which gaze velocity returns to a stable threshold</param>
        public static void SetDivergenceRecoverySpeed(this IUsesDetectGazeDivergence user, float rateAtWhichGazeVelocityReturnsToStableThreshold)
        {
#if !FI_AUTOFILL
            user.provider.SetDivergenceRecoverySpeed(rateAtWhichGazeVelocityReturnsToStableThreshold);
#endif
        }
    }
}
