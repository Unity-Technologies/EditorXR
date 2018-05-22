
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class the ability to place objects in the scene, or a MiniWorld
    /// </summary>
    public interface IPlaceSceneObject
    {
    }

    public static class IPlaceSceneObjectMethods
    {
        internal static Action<Transform, Vector3> placeSceneObject { get; set; }

        /// <summary>
        /// Method used to place objects in the scene/MiniWorld
        /// </summary>
        /// <param name="transform">Transform of the GameObject to place</param>
        /// <param name="scale">Target scale of placed object</param>
        public static void PlaceSceneObject(this IPlaceSceneObject obj, Transform transform, Vector3 scale)
        {
            placeSceneObject(transform, scale);
        }
    }
}

