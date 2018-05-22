
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides functionality that allows all UI interaction to be negated for a given rayOrigin
    /// </summary>
    public interface IBlockUIInteraction
    {
    }

    public static class IBlockUIInteractionMethods
    {
        internal static Action<Transform, bool> setUIBlockedForRayOrigin { get; set; }

        /// <summary>
        /// Prevent UI interaction for a given rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <param name="blocked">If true, UI interaction will be blocked for the rayOrigin.  If false, the ray origin will be removed from the blocked collection.</param>
        public static void SetUIBlockedForRayOrigin(this IBlockUIInteraction obj, Transform rayOrigin, bool blocked)
        {
            setUIBlockedForRayOrigin(rayOrigin, blocked);
        }
    }
}

