using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provides the ability block all UI interaction for a given rayOrigin
    /// </summary>
    public interface IProvidesBlockUIInteraction : IFunctionalityProvider
    {
        /// <summary>
        /// Prevent UI interaction for a given rayOrigin
        /// </summary>
        /// <param name="rayOrigin">The rayOrigin that is being checked</param>
        /// <param name="blocked">If true, UI interaction will be blocked for the rayOrigin.  If false, the ray origin will be removed from the blocked collection.</param>
        void SetUIBlockedForRayOrigin(Transform rayOrigin, bool blocked);
    }
}
