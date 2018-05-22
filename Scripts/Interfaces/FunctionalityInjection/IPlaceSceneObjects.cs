
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class the ability to place objects in the scene or a MiniWorld
    /// </summary>
    public interface IPlaceSceneObjects
    {
    }

    public static class IPlaceSceneObjectsMethods
    {
        internal static Action<Transform[], Vector3[], Quaternion[], Vector3[]> placeSceneObjects { get; set; }

        /// <summary>
        /// Method used to place groups of objects in the scene/MiniWorld
        /// </summary>
        /// <param name="transforms">Array of Transforms to place</param>
        /// <param name="targetPositionOffsets">Array of per-object target positions</param>
        /// <param name="targetRotations">Array of per-object target rotations</param>
        /// <param name="targetScales">Array of per-object target scales</param>
        public static void PlaceSceneObjects(this IPlaceSceneObjects obj, Transform[] transforms, Vector3[] targetPositionOffsets, Quaternion[] targetRotations, Vector3[] targetScales)
        {
            placeSceneObjects(transforms, targetPositionOffsets, targetRotations, targetScales);
        }
    }
}

