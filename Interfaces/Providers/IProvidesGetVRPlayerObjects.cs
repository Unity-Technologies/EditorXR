using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the VR Player objects
    /// </summary>
    public interface IProvidesGetVRPlayerObjects : IFunctionalityProvider
    {
        /// <summary>
        /// Returns objects that are used to represent the VR player
        /// </summary>
        /// <returns>A list containing the VR player objects</returns>
        List<GameObject> GetVRPlayerObjects();
    }
}
