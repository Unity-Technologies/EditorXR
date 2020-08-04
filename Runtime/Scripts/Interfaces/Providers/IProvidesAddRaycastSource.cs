using System;
using Unity.EditorXR.Interfaces;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provide the ability to add RaycastSources to the system
    /// </summary>
    interface IProvidesAddRaycastSource : IFunctionalityProvider
    {
        void AddRaycastSource(IProxy proxy, Node node, Transform rayOrigin, Func<IRaycastSource, bool> validationCallback = null);
    }
}
