
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives implementors the ability to check if an object contains the VR player completely
    /// </summary>
    public interface IContainsVRPlayerCompletely
    {
    }

    public static class IContainsVRPlayerCompletelyMethods
    {
        internal static Func<GameObject, bool> containsVRPlayerCompletely { get; set; }

        /// <summary>
        /// Returns objects that are used to represent the VR player
        /// </summary>
        public static bool ContainsVRPlayerCompletely(this IContainsVRPlayerCompletely obj, GameObject go)
        {
            return containsVRPlayerCompletely(go);
        }
    }
}

