using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to control ray visibility
    /// </summary>
    public interface IUsesRayVisibilitySettings : IFunctionalitySubscriber<IProvidesRayVisibilitySettings>
    {
    }

    public static class UsesRayVisibilitySettingsMethods
    {
        /// <summary>
        /// Add visibility settings to try and show/hide the ray/cone
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray to hide or show</param>
        /// <param name="caller">The object which is adding settings</param>
        /// <param name="rayVisible">Show or hide the ray</param>
        /// <param name="coneVisible">Show or hide the cone</param>
        /// <param name="priority">(Optional) The priority level of this request</param>
        public static void AddRayVisibilitySettings(this IUsesRayVisibilitySettings user, Transform rayOrigin,
            object caller, bool rayVisible, bool coneVisible, int priority = 0)
        {
#if !FI_AUTOFILL
            user.provider.AddRayVisibilitySettings(rayOrigin, caller, rayVisible, coneVisible, priority);
#endif
        }

        /// <summary>
        /// Remove visibility settings
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray from which to remove settings</param>
        /// <param name="caller">The object whose settings to remove</param>
        public static void RemoveRayVisibilitySettings(this IUsesRayVisibilitySettings user, Transform rayOrigin,
            object caller)
        {
#if !FI_AUTOFILL
            user.provider.RemoveRayVisibilitySettings(rayOrigin, caller);
#endif
        }
    }
}
