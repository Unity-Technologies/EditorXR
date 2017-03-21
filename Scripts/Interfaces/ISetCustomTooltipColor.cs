using UnityEditor.Experimental.EditorVR.Helpers;

#if UNITY_EDITOR
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
		GradientPair customToolTipHighlightColor { get; }
	}
}
#endif