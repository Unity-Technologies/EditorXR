using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the default ray color
    /// </summary>
    public interface IProvidesGetDefaultRayColor : IFunctionalityProvider
    {
        /// <summary>
        /// Get the color of the default ray
        /// </summary>
        /// <param name="rayOrigin">The ray whose color to get</param>
        /// <returns>The color of the default ray for the given ray origin</returns>
        Color GetDefaultRayColor(Transform rayOrigin);
    }
}
