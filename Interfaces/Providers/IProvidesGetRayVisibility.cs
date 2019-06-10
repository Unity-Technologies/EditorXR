using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to get the visibility of rays
    /// </summary>
    public interface IProvidesGetRayVisibility : IFunctionalityProvider
    {
        /// <summary>
        /// Returns whether the specified ray is visible
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is visible</returns>
        bool IsRayVisible(Transform rayOrigin);

        /// <summary>
        /// Returns whether the specified cone is visible
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the cone is visible</returns>
        bool IsConeVisible(Transform rayOrigin);
    }
}
