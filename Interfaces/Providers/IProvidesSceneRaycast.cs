using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to scene raycast functionality
    /// </summary>
    public interface IProvidesSceneRaycast : IFunctionalityProvider
    {
        /// <summary>
        /// Do a raycast against all Renderers
        /// </summary>
        /// <param name="ray">The ray to use for the raycast</param>
        /// <param name="hit">Hit information</param>
        /// <param name="go">The GameObject which was hit, if any</param>
        /// <param name="maxDistance">The maximum distance of the raycast</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>Whether the raycast hit a renderer</returns>
        bool Raycast(Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null);
    }
}
