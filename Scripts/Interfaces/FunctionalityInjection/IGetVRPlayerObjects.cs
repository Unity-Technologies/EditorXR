
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the gameobjects that represent the VR player
    /// </summary>
    public interface IGetVRPlayerObjects
    {
    }

    public static class IGetVRPlayerObjectsMethods
    {
        internal static Func<List<GameObject>> getVRPlayerObjects { get; set; }

        /// <summary>
        /// Returns objects that are used to represent the VR player
        /// </summary>
        public static List<GameObject> GetVRPlayerObjects(this IGetVRPlayerObjects obj)
        {
            return getVRPlayerObjects();
        }
    }
}

