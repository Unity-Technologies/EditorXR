using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to check if the main menu is visible
    /// </summary>
    public interface IProvidesIsMainMenuVisible : IFunctionalityProvider
    {
        /// <summary>
        /// Returns whether the main menu is visible on the specified rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the main menu is visible on the specified rayOrigin</returns>
        bool IsMainMenuVisible(Transform rayOrigin);
    }
}
