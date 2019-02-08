using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides CanGrabObject method used to check whether direct selection is possible on an object
    /// </summary>
    public interface ICanGrabObject
    {
    }

    public static class ICanGrabObjectMethods
    {
        internal static Func<GameObject, Transform, bool> canGrabObject { get; set; }

        /// <summary>
        /// Returns true if the object can be grabbed
        /// </summary>
        /// <param name="go">The selection</param>
        /// <param name="rayOrigin">The rayOrigin of the proxy that is looking to grab</param>
        public static bool CanGrabObject(this ICanGrabObject obj, GameObject go, Transform rayOrigin)
        {
            return canGrabObject(go, rayOrigin);
        }
    }
}
