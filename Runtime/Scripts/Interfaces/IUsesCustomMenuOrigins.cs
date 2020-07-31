using System;
using UnityEngine;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provides access to transform roots for custom menus
    /// </summary>
    public interface IUsesCustomMenuOrigins
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesCustomMenuOrigins
    /// </summary>
    public static class UsesCustomMenuOriginsMethods
    {
        internal static Func<Transform, Transform> getCustomMenuOrigin { get; set; }
        internal static Func<Transform, Transform> getCustomAlternateMenuOrigin { get; set; }

        /// <summary>
        /// Get the root transform for custom menus for a given ray origin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray origin for which we want custom the menu origin</param>
        /// <returns></returns>
        public static Transform GetCustomMenuOrigin(this IUsesCustomMenuOrigins user, Transform rayOrigin)
        {
            return getCustomMenuOrigin(rayOrigin);
        }

        /// <summary>
        /// Get the root transform for custom alternate menus for a given ray origin
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="rayOrigin">The ray origin for which we want the alternate menu origin</param>
        /// <returns></returns>
        public static Transform GetCustomAlternateMenuOrigin(this IUsesCustomMenuOrigins user, Transform rayOrigin)
        {
            return getCustomAlternateMenuOrigin(rayOrigin);
        }
    }
}
