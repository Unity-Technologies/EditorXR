using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the default ray color
    /// </summary>
    public interface IProvidesDetectGazeDivergence : IFunctionalityProvider
    {
      bool IsAboveDivergenceThreshold(Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true);

      void SetDivergenceRecoverySpeed(float rateAtWhichGazeVelocityReturnsToStableThreshold);
    }
}
