using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above a defined threshold, for a given transform's forward vector
    /// </summary>
    public interface IProvidesDetectGazeDivergence : IFunctionalityProvider
    {
      bool IsAboveDivergenceThreshold(Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true);

      void SetDivergenceRecoverySpeed(float rateAtWhichGazeVelocityReturnsToStableThreshold);
    }
}
