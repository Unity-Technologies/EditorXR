using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability to Enable/disable a given
    /// ray-origin's ability to intersect/interact with non UI objects
    /// </summary>
    public interface IProvidesControlInputIntersection : IFunctionalityProvider
    {
        /// <summary>
        /// Enable/disable a given ray-origin's ability to intersect/interact with non UI objects
        /// </summary>
        /// <param name="rayOrigin">RayOrigin to enable/disable</param>
        /// <param name="enabled">Enabled/disabled state of RayOrigin</param>
        void SetRayOriginEnabled(Transform rayOrigin, bool enabled);
    }
}
