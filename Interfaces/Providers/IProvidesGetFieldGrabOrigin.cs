using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to get preview origins
    /// </summary>
    public interface IProvidesGetFieldGrabOrigin : IFunctionalityProvider
    {
        /// <summary>
        /// Get the field grab transform attached to the given rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is grabbing the field</param>
        /// <returns>The field grab origin</returns>
        Transform GetFieldGrabOriginForRayOrigin(Transform rayOrigin);
    }
}
