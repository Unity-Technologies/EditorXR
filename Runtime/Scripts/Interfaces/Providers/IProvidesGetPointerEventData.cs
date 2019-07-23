using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Provide access to UI events
    /// </summary>
    interface IProvidesGetPointerEventData : IFunctionalityProvider
    {
        RayEventData GetPointerEventData(Transform rayOrigin);
    }
}
