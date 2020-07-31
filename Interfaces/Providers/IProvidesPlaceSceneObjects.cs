using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene placement functionality
    /// </summary>
    public interface IProvidesPlaceSceneObjects : IFunctionalityProvider
    {
        /// <summary>
        /// Place a group of objects in the scene/MiniWorld
        /// </summary>
        /// <param name="transforms">Array of Transforms to place</param>
        /// <param name="targetPositionOffsets">Array of per-object target positions</param>
        /// <param name="targetRotations">Array of per-object target rotations</param>
        /// <param name="targetScales">Array of per-object target scales</param>
        void PlaceSceneObjects(Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales);
    }
}
