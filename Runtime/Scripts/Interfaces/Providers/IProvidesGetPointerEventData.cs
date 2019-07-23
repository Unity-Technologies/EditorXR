using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Provide access to pointer event data
    /// </summary>
    interface IProvidesGetPointerEventData : IFunctionalityProvider
    {
        RayEventData GetPointerEventData(Transform rayOrigin);
    }
}
