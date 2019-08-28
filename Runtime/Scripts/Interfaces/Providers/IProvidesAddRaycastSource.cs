using System;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Provide the ability to add RaycastSources to the system
    /// </summary>
    interface IProvidesAddRaycastSource : IFunctionalityProvider
    {
        void AddRaycastSource(IProxy proxy, Node node, Transform rayOrigin, Func<IRaycastSource, bool> validationCallback = null);
    }
}
