#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to information about the 3D pointer
    /// </summary>
    interface IUsesPointer
    {
    }

    static class IUsesPointerMethods
    {
        internal static Func<Transform, float> getPointerLength { get; set; }

        /// <summary>
        /// Get the pointer length for a given ray origin
        /// </summary>
        /// <param name="rayOrigin">The ray origin whose pointer length to get</param>
        /// <returns>The pointer length</returns>
        public static float GetPointerLength(this IUsesPointer obj, Transform rayOrigin)
        {
            return getPointerLength(rayOrigin);
        }

        /// <summary>
        /// Get the position of the pointer for a given ray origin
        /// </summary>
        /// <param name="rayOrigin">The ray origin whose pointer position to get</param>
        /// <returns>The pointer position</returns>
        public static Vector3 GetPointerPosition(this IUsesPointer obj, Transform rayOrigin)
        {
            return rayOrigin.position + rayOrigin.forward * obj.GetPointerLength(rayOrigin);
        }
    }
}
#endif
