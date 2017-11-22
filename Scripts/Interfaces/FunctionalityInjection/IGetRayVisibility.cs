#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to checks that can test whether parts of the default ray are visible
    /// </summary>
    public interface IGetRayVisibility
    {
    }

    public static class IGetRayVisibilityMethods
    {
        internal static Func<Transform, bool> isRayVisible { get; set; }
        internal static Func<Transform, bool> isConeVisible { get; set; }

        /// <summary>
        /// Returns whether the specified ray is visible
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsRayVisible(this IGetRayVisibility obj, Transform rayOrigin)
        {
            return isRayVisible(rayOrigin);
        }

        /// <summary>
        /// Returns whether the specified cone is visible
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsConeVisible(this IGetRayVisibility obj, Transform rayOrigin)
        {
            return isConeVisible(rayOrigin);
        }
    }
}
#endif

