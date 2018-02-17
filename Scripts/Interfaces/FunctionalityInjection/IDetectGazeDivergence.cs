#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class ability to detect gaze divergence above an defined threshold, for a given transform's forward vector
    /// 
    /// Spatially scrolling allows for directional input-device movement to drive changes/progression of UI
    /// element selection, without the need for additional input beyond the movement of an input-device.
    /// </summary>
    public interface IDetectGazeDivergence
    {
    }

    public static class IDetectGazeDivergenceMethods
    {
        internal delegate bool IsAboveDivergenceThresholdDelegate(IDetectGazeDivergence obj, Vector3 directionToTest, float divergenceThreshold);

        internal static Func<Vector3, float, bool> isAboveDivergenceThreshold { private get; set; }

        public static bool IsAboveDivergenceThreshold(this IDetectGazeDivergence obj, Vector3 directionToTest, float divergenceThreshold)
        {
            return isAboveDivergenceThreshold(directionToTest, divergenceThreshold);
        }
    }
}
#endif
