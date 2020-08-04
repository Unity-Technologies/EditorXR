using System;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to tool selection
    /// </summary>
    public interface IProvidesSelectTool : IFunctionalityProvider
    {
        /// <summary>
        /// Select tools from the menu
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that the tool should spawn under</param>
        /// <param name="toolType">Type of tool to spawn/select</param>
        /// <param name="despawnOnReselect">Despawn the tool, if re-selected while already the current tool</param>
        /// <param name="hideMenu">Whether to hide the menu after selecting this tool</param>
        /// <returns>Whether the tool was successfully selected</returns>
        bool SelectTool(Transform rayOrigin, Type toolType, bool despawnOnReselect = true, bool hideMenu = false);

        /// <summary>
        /// Check if the active tool on the given ray origin is of the given type
        /// </summary>
        /// <param name="rayOrigin">The ray origin to check</param>
        /// <param name="type">The tool type to compare</param>
        /// <returns>True if the active tool is of the given type</returns>
        bool IsToolActive(Transform rayOrigin, Type type);
    }
}
