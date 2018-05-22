
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to checks that can test whether a ray is hovering over a UI element
    /// </summary>
    public interface IIsHoveringOverUI
    {
    }

    public static class IIsHoveringOverUIMethods
    {
        internal static Func<Transform, bool> isHoveringOverUI { get; set; }

        /// <summary>
        /// Returns whether the specified ray origin is hovering over a UI element
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsHoveringOverUI(this IIsHoveringOverUI obj, Transform rayOrigin)
        {
            return isHoveringOverUI(rayOrigin);
        }
    }
}

