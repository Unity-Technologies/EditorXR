#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    ///
    /// </summary>
    public interface IControlInputIntersection
    {
    }

    public static class IControlInputIntersectionMethods
    {
        internal static Action<Transform, bool> setRayOriginEnabled { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static void SetRayOriginEnabled(this IControlInputIntersection obj, Transform rayOrigin, bool enabled)
        {
            setRayOriginEnabled(rayOrigin, enabled);
        }
    }
}
#endif
