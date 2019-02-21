using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to checks that can test whether the main menu is visible on a given ray origin
    /// </summary>
    public interface IIsMainMenuVisible
    {
    }

    public static class IIsMainMenuVisibleMethods
    {
        internal static Func<Transform, bool> isMainMenuVisible { get; set; }

        /// <summary>
        /// Returns whether the main menu is visible on the specified rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsMainMenuVisible(this IIsMainMenuVisible obj, Transform rayOrigin)
        {
            return isMainMenuVisible(rayOrigin);
        }
    }
}
