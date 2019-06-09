using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    public interface IProvidesRaycastResults : IFunctionalityProvider
    {
        /// <summary>
        /// Method used to test hover/intersection
        /// Returns the first GameObject being hovered over, or intersected with
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin for intersection purposes</param>
        GameObject GetFirstGameObject(Transform rayOrigin);
    }
}
