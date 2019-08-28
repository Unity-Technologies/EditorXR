using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the VR Player objects
    /// </summary>
    public interface IProvidesGetVRPlayerObjects : IFunctionalityProvider
    {
        /// <summary>
        /// Returns objects that are used to represent the VR player
        /// </summary>
        List<GameObject> GetVRPlayerObjects();
    }
}
