using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability to check if an object can be grabbed
    /// </summary>
    public interface IProvidesCanGrabObject : IFunctionalityProvider
    {
        /// <summary>
        /// Returns true if the object can be grabbed
        /// </summary>
        /// <param name="gameObject">The selection</param>
        /// <param name="rayOrigin">The rayOrigin of the proxy that is looking to grab</param>
        /// <returns>True if the object can be grabbed</returns>
        bool CanGrabObject(GameObject gameObject, Transform rayOrigin);
    }
}
