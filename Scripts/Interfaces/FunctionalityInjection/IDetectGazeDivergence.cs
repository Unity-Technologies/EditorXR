#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above an defined threshold, for a given transform's forward vector
    /// </summary>
    public interface IDetectGazeDivergence
    {
        //float gazeIsStable { get; }
    }

    public static class IDetectGazeDivergenceMethods
    {
        internal delegate bool IsAboveDivergenceThresholdDelegate(IDetectGazeDivergence obj, Transform transformToTest, float divergenceThreshold, bool detectIfGazeIsUnstable = false);

        internal static Func<Transform, float, bool, bool> isAboveDivergenceThreshold { private get; set; }

        public static bool IsAboveDivergenceThreshold(this IDetectGazeDivergence obj, Transform transformToTest, float divergenceThreshold, bool detectIfGazeIsUnstable = false)
        {
            return isAboveDivergenceThreshold(transformToTest, divergenceThreshold, detectIfGazeIsUnstable);
        }
    }
}
#endif
