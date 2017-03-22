#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Provides access to the ability to show or hide a Tooltip
	/// </summary>
	public interface ISetTooltipVisibility
	{
	}

	public static class ISetTooltipVisibilityMethods
	{
		internal static Action<ITooltip> showTooltip { get; set; }
		internal static Action<ITooltip> hideTooltip { get; set; }

		/// <summary>
		/// Show the given Tooltip
		/// </summary>
		public static void ShowTooltip(this ISetTooltipVisibility obj, ITooltip tooltip)
		{
			if (showTooltip != null)
				showTooltip(tooltip);
		}

		/// <summary>
		/// Hide the given Tooltip
		/// </summary>
		public static void HideTooltip(this ISetTooltipVisibility obj, ITooltip tooltip)
		{
			if (hideTooltip != null)
				hideTooltip(tooltip);
		}

	}
}
#endif
