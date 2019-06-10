using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to check if a ray is hovering over UI
    /// </summary>
    public interface IProvidesIsHoveringOverUI : IFunctionalityProvider
    {
        /// <summary>
        /// Returns whether the specified ray origin is hovering over a UI element
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is hovering over UI</returns>
        bool IsHoveringOverUI(Transform rayOrigin);
    }
}
