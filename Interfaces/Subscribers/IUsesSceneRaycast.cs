using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class access to IntersectionModule.Raycast
    /// </summary>
    public interface IUsesSceneRaycast : IFunctionalitySubscriber<IProvidesSceneRaycast>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesSceneRaycast
    /// </summary>
    public static class UsesSceneRaycastMethods
    {
        /// <summary>
        /// Do a raycast against all Renderers
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="ray">The ray to use for the raycast</param>
        /// <param name="hit">Hit information</param>
        /// <param name="go">The GameObject which was hit, if any</param>
        /// <param name="maxDistance">The maximum distance of the raycast</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>Whether the raycast hit a renderer</returns>
        public static bool Raycast(this IUsesSceneRaycast user, Ray ray, out RaycastHit hit, out GameObject go, float maxDistance = Mathf.Infinity, List<GameObject> ignoreList = null)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.Raycast(ray, out hit, out go, maxDistance, ignoreList);
#endif
        }
    }
}
