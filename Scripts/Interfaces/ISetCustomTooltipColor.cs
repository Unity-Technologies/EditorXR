#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Helpers;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Decorates classes which can specify custom tool tip colors
    /// </summary>
    public interface ISetCustomTooltipColor
    {
        /// <summary>
        /// Custom tooltip highlight color
        /// </summary>
        GradientPair customTooltipHighlightColor { get; }
    }
}
#endif
