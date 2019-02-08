using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class access to hover/intersection detection
    /// </summary>
    public interface IUsesRaycastResults
    {
    }

    public static class IUsesRaycastResultsMethods
    {
        internal static Func<Transform, GameObject> getFirstGameObject { get; set; }

        /// <summary>
        /// Method used to test hover/intersection
        /// Returns the first GameObject being hovered over, or intersected with
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin for intersection purposes</param>
        public static GameObject GetFirstGameObject(this IUsesRaycastResults obj, Transform rayOrigin)
        {
            return getFirstGameObject(rayOrigin);
        }
    }
}
