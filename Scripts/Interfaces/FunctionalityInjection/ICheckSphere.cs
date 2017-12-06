#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Gives decorated class access to IntersectionModule.CheckSphere
    /// </summary>
    public interface ICheckSphere
    {
    }

    public static class ICheckSphereMethods
    {
        public delegate bool CheckSphereDelegate(Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null);

        public static CheckSphereDelegate checkSphere { get; set; }

        /// <summary>
        /// Do a sphere check against all Renderers
        /// </summary>
        /// <param name="center">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="objects">The list to which intersected Renderers will be added</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>Whether the sphere intersected any objects</returns>
        public static bool CheckSphere(this ICheckBounds obj, Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
            return checkSphere(center, radius, objects, ignoreList);
        }
    }
}
#endif
