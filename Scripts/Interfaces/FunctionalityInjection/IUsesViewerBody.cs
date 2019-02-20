using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to checks that can test against the viewer's body
    /// </summary>
    public interface IUsesViewerBody
    {
    }

    public static class IUsesViewerBodyMethods
    {
        internal static Func<Transform, bool> isOverShoulder { get; set; }
        internal static Func<Transform, bool> isAboveHead { get; set; }

        /// <summary>
        /// Returns whether the specified transform is over the viewer's shoulders and behind the head
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        public static bool IsOverShoulder(this IUsesViewerBody obj, Transform rayOrigin)
        {
            return isOverShoulder(rayOrigin);
        }

        /// <summary>
        /// Returns whether the specified transform is over the viewer's head
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin to test</param>
        public static bool IsAboveHead(this IUsesViewerBody obj, Transform rayOrigin)
        {
            return isAboveHead(rayOrigin);
        }
    }
}
