#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Gives decorated class ability to control spatial-hinting visuals
	/// </summary>
	public interface IControlSpatialHinting
	{
		/// <summary>
		/// Description
		/// </summary>
		bool spatialHintVisualsVisible { get; set; }

		/// <summary>
		/// Enables/disables the visual elements that should be shown when beginning to initiate a spatial selection action
		/// This is only enabled before the enabling of the main select visuals
		/// </summary>
		bool spatialHintPreScrollVisualsVisible { set; }

		/// <summary>
		/// Description
		/// </summary>
		bool spatialHintPrimaryArrowsVisible { set; }

		/// <summary>
		/// Description
		/// </summary>
		bool spatialHintSecondaryArrowsVisible { set; }

		/// <summary>
		/// Description
		/// </summary>
		Vector3 spatialHintScrollVisualsRotation { set; }

		/// <summary>
		/// Description
		/// </summary>
		Vector3 spatialHintScrollVisualsDragThresholdTriggerPosition { set; }

		/// <summary>
		/// Description
		/// </summary>
		Transform spatialHintContentContainer { get; set; }
	}

	public static class IControlSpatialHintingMethods
	{
		internal delegate void PulseScrollArrowsDelegate();
		internal static PulseScrollArrowsDelegate pulseScrollArrows { get; set; }

		/// <summary>
		/// Visually pulse the spatial-scroll arrows; the arrows shown when performing a spatiatil scroll
		/// </summary>
		public static void PulseScrollArrows(this IControlSpatialHinting obj)
		{
			pulseScrollArrows();
		}
	}
}
#endif
