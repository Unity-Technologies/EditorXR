using UnityEngine;

namespace Unity.Labs.EditorXR.Extensions
{
    static class BoundsExtensions
    {
        /// <summary>
        /// Returns a whether the given bounds are contained completely within this one
        /// </summary>
        /// <param name="otherBounds">The bounds to compare with this one</param>
        public static bool ContainsCompletely(this Bounds bounds, Bounds otherBounds)
        {
            var boundsMin = bounds.min;
            var boundsMax = bounds.max;
            var otherBoundsMin = otherBounds.min;
            var otherBoundsMax = otherBounds.max;
            return boundsMax.x >= otherBoundsMax.x && boundsMax.y >= otherBoundsMax.y && boundsMax.z >= otherBoundsMax.z
                   && boundsMin.x <= otherBoundsMin.x && boundsMin.y <= otherBoundsMin.y && boundsMin.z <= otherBoundsMin.z;
        }
    }
}
