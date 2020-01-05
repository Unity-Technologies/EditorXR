using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above a defined threshold, for a given transform's forward vector
    /// </summary>
    public interface IProvidesDetectGazeDivergence : IFunctionalityProvider
    {
        /// <summary>
        /// Check if gaze direction is above the divergence threshold
        /// </summary>
        /// <param name="transformToTest">The transform representing gaze direction</param>
        /// <param name="divergenceThreshold">The threshold angle value to test</param>
        /// <param name="disregardTemporalStability">Whether to disregard temporal stability</param>
        /// <returns>True if the angle between the gaze and target is above the divergence threshold</returns>
        bool IsAboveDivergenceThreshold(Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true);

        /// <summary>
        /// Set the divergence recovery speed
        /// </summary>
        /// <param name="rateAtWhichGazeVelocityReturnsToStableThreshold">The rate at which gaze velocity returns to a stable threshold</param>
        void SetDivergenceRecoverySpeed(float rateAtWhichGazeVelocityReturnsToStableThreshold);
    }
}
