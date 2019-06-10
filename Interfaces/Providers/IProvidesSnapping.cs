using Unity.Labs.ModuleLoader;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    public interface IProvidesSnapping : IFunctionalityProvider
    {
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
        bool ManipulatorSnap(Transform rayOrigin, Transform[] transforms, ref Vector3 position, ref Quaternion rotation,
            Vector3 delta, AxisFlags constraints, PivotMode pivotMode);

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
        bool DirectSnap(Transform rayOrigin, Transform transform, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation);

        /// <summary>
        /// Clear state information for a given ray
        /// </summary>
        /// <param name="rayOrigin">The ray whose state to clear</param>
        void ClearSnappingState(Transform rayOrigin);
    }
}
