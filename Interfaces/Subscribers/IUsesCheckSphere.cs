using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Gives decorated class the ability to check if scene objects are contained within a sphere
    /// </summary>
    public interface IUsesCheckSphere : IFunctionalitySubscriber<IProvidesCheckSphere>
    {
    }

    /// <summary>
    /// Extension methods for implementors of IUsesCheckSphere
    /// </summary>
    public static class UsesCheckSphereMethods
    {
        /// <summary>
        /// Do a sphere check against all Renderers
        /// </summary>
        /// <param name="user">The functionality user</param>
        /// <param name="center">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="objects">The list to which intersected Renderers will be added</param>
        /// <param name="ignoreList">(optional) A list of Renderers to ignore</param>
        /// <returns>True if the sphere intersected any objects</returns>
        public static bool CheckSphere(this IUsesCheckSphere user, Vector3 center, float radius, List<GameObject> objects, List<GameObject> ignoreList = null)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return user.provider.CheckSphere(center, radius, objects, ignoreList);
#endif
        }
    }
}
