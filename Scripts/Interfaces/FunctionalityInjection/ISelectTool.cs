#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class the ability to select tools from a menu
    /// </summary>
    public interface ISelectTool
    {
    }

    public static class ISelectToolMethods
    {
        internal static Func<Transform, Type, bool, bool, bool, bool> selectTool { get; set; }
        internal static Func<Transform, Type, bool> isToolActive { get; set; }

        /// <summary>
        /// Method used to select tools from the menu
        /// Returns whether the tool was successfully selected
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that the tool should spawn under</param>
        /// <param name="toolType">Type of tool to spawn/select</param>
        /// <param name="despawnOnReselect">Despawn the tool, if re-selected while already the current tool</param>
        /// <param name="hideMenu">Whether to hide the menu after selecting this tool</param>
        public static bool SelectTool(this ISelectTool obj, Transform rayOrigin, Type toolType, bool despawnOnReselect = true, bool hideMenu = false, bool setSelectAsCurrentToolOnDespawn = true)
        {
            return selectTool(rayOrigin, toolType, despawnOnReselect, hideMenu, setSelectAsCurrentToolOnDespawn);
        }

        /// <summary>
        /// Returns true if the active tool on the given ray origin is of the given type
        /// </summary>
        /// <param name="rayOrigin">The ray origin to check</param>
        /// <param name="type">The tool type to compare</param>
        public static bool IsToolActive(this ISelectTool obj, Transform rayOrigin, Type type)
        {
            return isToolActive(rayOrigin, type);
        }
    }
}
#endif
