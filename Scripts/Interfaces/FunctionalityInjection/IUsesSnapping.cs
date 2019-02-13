using System;
using UnityEngine;

#if !UNITY_EDITOR
public enum PivotMode
{
    Center,
    Pivot,
}
#endif

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class the ability to use snapping
    /// </summary>
    public interface IUsesSnapping
    {
    }

    public static class IUsesSnappingMethods
    {
        internal delegate bool ManipulatorSnapDelegate(Transform rayOrigin, Transform[] transforms, ref Vector3 position, ref Quaternion rotation, Vector3 delta, AxisFlags constraints, PivotMode pivotMode);

        internal delegate bool DirectSnapDelegate(Transform rayOrigin, Transform transform, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation);

        internal static ManipulatorSnapDelegate manipulatorSnap { get; set; }
        internal static DirectSnapDelegate directSnap { get; set; }
        internal static Action<Transform> clearSnappingState { get; set; }

        /// <summary>
        /// Perform manipulator snapping: Translate a position vector using deltas while also respecting snapping
        /// </summary>
        /// <param name="rayOrigin">The ray doing the translating</param>
        /// <param name="transforms">The transforms being translated (used to determine bounds; Transforms do not get modified)</param>
        /// <param name="position">The position being modified by delta. This will be set with a snapped position if possible</param>
        /// <param name="rotation">The rotation to be modified if rotation snapping is enabled</param>
        /// <param name="delta">The position delta to apply</param>
        /// <param name="constraints">The axis constraints</param>
        /// <param name="pivotMode">The current pivot mode</param>
        /// <returns>Whether the position was set to a snapped position</returns>
        public static bool ManipulatorSnap(this IUsesSnapping usesSnaping, Transform rayOrigin, Transform[] transforms, ref Vector3 position, ref Quaternion rotation, Vector3 delta, AxisFlags constraints, PivotMode pivotMode)
        {
            return manipulatorSnap(rayOrigin, transforms, ref position, ref rotation, delta, constraints, pivotMode);
        }

        /// <summary>
        /// Perform direct snapping: Transform a position/rotation directly while also respecting snapping
        /// </summary>
        /// <param name="rayOrigin">The ray doing the transforming</param>
        /// <param name="transform">The object being transformed (used to determine bounds; Transforms do not get modified)</param>
        /// <param name="position">The position being transformed. This will be set to a snapped position if possible</param>
        /// <param name="rotation">The rotation being transformed. This will only be modified if rotation snapping is enabled</param>
        /// <param name="targetPosition">The input position provided by direct transformation</param>
        /// <param name="targetRotation">The input rotation provided by direct transformation</param>
        /// <returns></returns>
        public static bool DirectSnap(this IUsesSnapping usesSnaping, Transform rayOrigin, Transform transform, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
        {
            return directSnap(rayOrigin, transform, ref position, ref rotation, targetPosition, targetRotation);
        }

        /// <summary>
        /// Clear state information for a given ray
        /// </summary>
        /// <param name="rayOrigin">The ray whose state to clear</param>
        public static void ClearSnappingState(this IUsesSnapping usesSnaping, Transform rayOrigin)
        {
            clearSnappingState(rayOrigin);
        }
    }
}
