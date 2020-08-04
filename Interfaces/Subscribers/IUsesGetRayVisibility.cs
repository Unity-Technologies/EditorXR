using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to get the visibility of rays
    /// </summary>
    public interface IUsesGetRayVisibility : IFunctionalitySubscriber<IProvidesGetRayVisibility>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesGetRayVisibility
    /// </summary>
    public static class UsesGetRayVisibilityMethods
    {
        /// <summary>
        /// Returns whether the specified ray is visible
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is visible</returns>
        public static bool IsRayVisible(this IUsesGetRayVisibility user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsRayVisible(rayOrigin);
#endif
        }

        /// <summary>
        /// Returns whether the specified cone is visible
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the cone is visible</returns>
        public static bool IsConeVisible(this IUsesGetRayVisibility user, Transform rayOrigin)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.IsConeVisible(rayOrigin);
#endif
        }
    }
}
