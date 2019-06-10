using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to control ray visibility
    /// </summary>
    public interface IProvidesRayVisibilitySettings : IFunctionalityProvider
    {
        /// <summary>
        /// Add visibility settings to try and show/hide the ray/cone
        /// </summary>
        /// <param name="rayOrigin">The ray to hide or show</param>
        /// <param name="caller">The object which is adding settings</param>
        /// <param name="rayVisible">Show or hide the ray</param>
        /// <param name="coneVisible">Show or hide the cone</param>
        /// <param name="priority">(Optional) The priority level of this request</param>
        void AddRayVisibilitySettings(Transform rayOrigin, object caller, bool rayVisible, bool coneVisible, int priority = 0);

        /// <summary>
        /// Remove visibility settings
        /// </summary>
        /// <param name="rayOrigin">The ray from which to remove settings</param>
        /// <param name="caller">The object whose settings to remove</param>
        void RemoveRayVisibilitySettings(Transform rayOrigin, object caller);
    }
}
