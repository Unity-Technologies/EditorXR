using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above a defined threshold, for a given transform's forward vector
    /// </summary>
    public interface IDetectGazeDivergence
    {
    }

    public static class IDetectGazeDivergenceMethods
    {
        internal delegate bool IsAboveDivergenceThresholdDelegate(IDetectGazeDivergence obj, Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true);

        internal static Func<Transform, float, bool, bool> isAboveDivergenceThreshold { private get; set; }

        public static bool IsAboveDivergenceThreshold(this IDetectGazeDivergence obj, Transform transformToTest, float divergenceThreshold, bool disregardTemporalStability = true)
        {
            return isAboveDivergenceThreshold(transformToTest, divergenceThreshold, disregardTemporalStability);
        }
    }
}
