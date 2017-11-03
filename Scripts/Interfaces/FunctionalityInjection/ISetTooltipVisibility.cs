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
        internal static Action<ITooltip, bool, float, ITooltipPlacement> showTooltip { get; set; }
        internal static Action<ITooltip, bool> hideTooltip { get; set; }

        /// <summary>
        /// Show the given Tooltip
        /// </summary>
        /// <param name="tooltip">The tooltip to show</param>
        /// <param name="persistent">Whether the tooltip should stay visible regardless of raycasts</param>
        /// <param name="duration">If the tooltip is shown persistently, and duration is > 0, hide after the duration, in seconds</param>
        /// <param name="placement">If the tooltip is shown persistently, and duration is > 0, hide after the duration, in seconds</param>
        public static void ShowTooltip(this ISetTooltipVisibility obj, ITooltip tooltip, bool persistent = false,
            float duration = 0f, ITooltipPlacement placement = null)
        {
            showTooltip(tooltip, persistent, duration, placement);
        }

        /// <summary>
        /// Hide the given Tooltip
        /// </summary>
        /// <param name="tooltip">The tooltip to hide</param>
        /// <param name="persistent">Whether to hide the tooltip if it was shown with the persistent argument set to true</param>
        public static void HideTooltip(this ISetTooltipVisibility obj, ITooltip tooltip, bool persistent = false)
        {
            hideTooltip(tooltip, persistent);
        }
    }
}
#endif
