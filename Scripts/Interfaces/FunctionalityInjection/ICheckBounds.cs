#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class access to IntersectionModule.CheckBounds
    /// </summary>
    public interface ICheckBounds
    {
    }

    public static class ICheckBoundsMethods
    {
        public delegate bool CheckBoundsDelegate(Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null);

        public static CheckBoundsDelegate checkBounds { get; set; }

        /// <summary>
        /// Do a bounds check against all Renderers
        /// </summary>
        /// <param name="bounds">The bounds against which to test for Renderers</param>
        /// <param name="objects">The list to which intersected Renderers will be added</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>Whether the bounds intersected any objects</returns>
        public static bool CheckBounds(this ICheckBounds obj, Bounds bounds, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            return checkBounds(bounds, objects, ignoreList);
        }
    }
}
#endif
