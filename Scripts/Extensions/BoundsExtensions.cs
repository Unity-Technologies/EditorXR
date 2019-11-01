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
            return bounds.max.x >= otherBounds.max.x && bounds.max.y >= otherBounds.max.y && bounds.max.z >= otherBounds.max.z
                   && bounds.min.x <= otherBounds.min.x && bounds.min.y <= otherBounds.min.y && bounds.min.z <= otherBounds.min.z;
        }
    }
}
