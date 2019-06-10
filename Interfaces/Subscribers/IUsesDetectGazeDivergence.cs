using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to the default ray color
    /// </summary>
    public interface IUsesDetectGazeDivergence : IFunctionalitySubscriber<IProvidesDetectGazeDivergence>
    {
    }

    public static class UsesDetectGazeDivergenceMethods
    {
      public static bool IsAboveDivergenceThreshold(this IUsesDetectGazeDivergence user, Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true)
      {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsAboveDivergenceThreshold(transformToTest, divergenceThreshold, disregardTemporalStability);
#endif
      }

      public static void SetDivergenceRecoverySpeed(this IUsesDetectGazeDivergence user, float rateAtWhichGazeVelocityReturnsToStableThreshold)
      {
#if !FI_AUTOFILL
            user.provider.SetDivergenceRecoverySpeed(rateAtWhichGazeVelocityReturnsToStableThreshold);
#endif
      }
    }
}
