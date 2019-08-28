using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to check if scene objects are contained within a sphere
    /// </summary>
    public interface IProvidesCheckSphere : IFunctionalityProvider
    {
        /// <summary>
        /// Do a sphere check against all Renderers
        /// </summary>
        /// <param name="center">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="objects">The list to which intersected Renderers will be added</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>True if the sphere intersected any objects</returns>
        bool CheckSphere(Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null);
    }
}
