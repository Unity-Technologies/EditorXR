using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to tool selection
    /// </summary>
    public interface IUsesSelectTool : IFunctionalitySubscriber<IProvidesSelectTool>
    {
    }

    public static class UsesSelectToolMethods
    {
        /// <summary>
        /// Method used to select tools from the menu
        /// Returns whether the tool was successfully selected
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that the tool should spawn under</param>
        /// <param name="toolType">Type of tool to spawn/select</param>
        /// <param name="despawnOnReselect">Despawn the tool, if re-selected while already the current tool</param>
        /// <param name="hideMenu">Whether to hide the menu after selecting this tool</param>
        public static bool SelectTool(this IUsesSelectTool user, Transform rayOrigin, Type toolType, bool despawnOnReselect = true, bool hideMenu = false)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.SelectTool(rayOrigin, toolType, despawnOnReselect, hideMenu);
#endif
        }

        /// <summary>
        /// Returns true if the active tool on the given ray origin is of the given type
        /// </summary>
        /// <param name="rayOrigin">The ray origin to check</param>
        /// <param name="type">The tool type to compare</param>
        public static bool IsToolActive(this IUsesSelectTool user, Transform rayOrigin, Type type)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsToolActive(rayOrigin, type);
#endif
        }
    }
}
