using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to checks that can test whether a rayOrigin is contained in a miniworld
    /// </summary>
    public interface IIsInMiniWorld
    {
    }

    public static class IIsInMiniWorldMethods
    {
        internal static Func<Transform, bool> isInMiniWorld { get; set; }

        /// <summary>
        /// Returns whether the specified ray is contained in a miniworld
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        public static bool IsInMiniWorld(this IIsInMiniWorld obj, Transform rayOrigin)
        {
            return isInMiniWorld(rayOrigin);
        }
    }
}
