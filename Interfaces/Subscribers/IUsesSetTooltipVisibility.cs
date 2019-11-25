using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class control of tooltip visibility
    /// </summary>
    public interface IUsesSetTooltipVisibility : IFunctionalitySubscriber<IProvidesSetTooltipVisibility>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesSetTooltipVisibility
    /// </summary>
    public static class UsesSetTooltipVisibilityMethods
    {
        /// <summary>
        /// Show a Tooltip. Calling ShowTooltip on an ITooltip that was just shown will update its placement and timing
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="tooltip">The tooltip to show</param>
        /// <param name="persistent">Whether the tooltip should stay visible regardless of raycasts</param>
        /// <param name="duration">If the tooltip is shown persistently, and duration is less than 0, hide after the
        /// duration, in seconds. If duration greater than 0, placement is updated but timing is not affected. If
        /// duration is exactly 0, tooltip stays visible until explicitly hidden</param>
        /// <param name="placement">(Optional) The ITooltipPlacement object used to place the tooltip. If no placement
        /// is specified, we assume the ITooltip is a component and use its own Transform</param>
        /// <param name="becameVisible">(Optional) Called as soon as the tooltip becomes visible</param>
        public static void ShowTooltip(this IUsesSetTooltipVisibility user, ITooltip tooltip, bool persistent = false,
            float duration = 0f, ITooltipPlacement placement = null, Action becameVisible = null)
        {
#if !FI_AUTOFILL
            user.provider.ShowTooltip(tooltip, persistent, duration, placement, becameVisible);
#endif
        }

        /// <summary>
        /// Hide the given Tooltip
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="tooltip">The tooltip to hide</param>
        /// <param name="persistent">Whether to hide the tooltip if it was shown with the persistent argument set to true</param>
        public static void HideTooltip(this IUsesSetTooltipVisibility user, ITooltip tooltip, bool persistent = false)
        {
#if !FI_AUTOFILL
            user.provider.HideTooltip(tooltip, persistent);
#endif
        }
    }
}
