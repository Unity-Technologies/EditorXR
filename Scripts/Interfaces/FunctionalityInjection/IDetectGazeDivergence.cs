#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Modules;

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
        /// <summary>
        /// The data defining an element whose worldspace vector will be tested against the gaze-forward-vector
        /// </summary>
        GazeDivergenceModule.GazeDivergenceData gazeDivergenceData { get; set; }
    }

    public static class IDetectGazeDivergenceMethods
    {
        internal delegate bool IsAboveDivergenceThresholdDelegate(IDetectGazeDivergence obj, GazeDivergenceModule.GazeDivergenceData data);

        internal static IsAboveDivergenceThresholdDelegate isAboveDivergenceThreshold { private get; set; }

        public static bool IsAboveDivergenceThreshold(this IDetectGazeDivergence obj, GazeDivergenceModule.GazeDivergenceData data)
        {
            return isAboveDivergenceThreshold(obj, data);
        }
    }
}
#endif
