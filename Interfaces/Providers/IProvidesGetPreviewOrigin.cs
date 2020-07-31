using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to get preview origins
    /// </summary>
    public interface IProvidesGetPreviewOrigin : IFunctionalityProvider
    {
        /// <summary>
        /// Get the preview transform attached to the given rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin where the preview will occur</param>
        /// <returns>The preview origin</returns>
        Transform GetPreviewOriginForRayOrigin(Transform rayOrigin);
    }
}
