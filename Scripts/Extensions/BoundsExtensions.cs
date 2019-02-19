using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Extensions
{
    static class BoundsExtensions
    {
        /// <summary>
        /// Returns a whether the given bounds are contained completely within this one
        /// </summary>
        /// <param name="otherBounds">The bounds to compare with this one</param>
        public static bool ContainsCompletely(this Bounds bounds, Bounds otherBounds)
        {
            if (bounds.min.MinComponent() > otherBounds.min.MinComponent()
                || bounds.max.MinComponent() < otherBounds.max.MinComponent())
                return false;

            return true;
        }
    }
}
