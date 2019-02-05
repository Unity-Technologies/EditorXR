#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class the ability to Enable/disable a given
    /// ray-origin's ability to intersect/interact with non UI objects
    /// </summary>
    public interface IControlInputIntersection
    {
    }

    public static class IControlInputIntersectionMethods
    {
        internal static Action<Transform, bool> setRayOriginEnabled { private get; set; }

        /// <summary>
        /// Enable/disable a given ray-origin's ability to intersect/interact with non UI objects
        /// </summary>
        /// <param name="rayOrigin">RayOrigin to enable/disable</param>
        /// <param name="enabled">Enabled/disabled state of RayOrigin</param>
        public static void SetRayOriginEnabled(this IControlInputIntersection obj, Transform rayOrigin, bool enabled)
        {
            setRayOriginEnabled(rayOrigin, enabled);
        }
    }
}
#endif
