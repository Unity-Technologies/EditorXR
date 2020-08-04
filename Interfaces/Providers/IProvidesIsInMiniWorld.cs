using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to check if a RayOrigin is in a MiniWorld
    /// </summary>
    public interface IProvidesIsInMiniWorld : IFunctionalityProvider
    {
        /// <summary>
        /// Returns whether the specified ray is contained in a MiniWorld
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <returns>Whether the ray is contained in a MiniWorld</returns>
        bool IsInMiniWorld(Transform rayOrigin);
    }
}
