using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to set the color of the default ray
    /// </summary>
    public interface IProvidesSetDefaultRayColor : IFunctionalityProvider
    {
        /// <summary>
        /// Set the color of the default ray
        /// </summary>
        /// <param name="rayOrigin">The ray on which to set the color</param>
        /// <param name="color">The color to set on the default ray</param>
        void SetDefaultRayColor(Transform rayOrigin, Color color);
    }
}
