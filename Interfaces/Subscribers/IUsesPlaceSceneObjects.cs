using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to scene placement functionality
    /// </summary>
    public interface IUsesPlaceSceneObjects : IFunctionalitySubscriber<IProvidesPlaceSceneObjects>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesPlaceSceneObjects
    /// </summary>
    public static class UsesPlaceSceneObjectsMethods
    {
        /// <summary>
        /// Method used to place groups of objects in the scene/MiniWorld
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="transforms">Array of Transforms to place</param>
        /// <param name="targetPositionOffsets">Array of per-object target positions</param>
        /// <param name="targetRotations">Array of per-object target rotations</param>
        /// <param name="targetScales">Array of per-object target scales</param>
        public static void PlaceSceneObjects(this IUsesPlaceSceneObjects user, Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
#if !FI_AUTOFILL
            user.provider.PlaceSceneObjects(transforms, targetPositionOffsets, targetRotations, targetScales);
#endif
        }
    }
}
