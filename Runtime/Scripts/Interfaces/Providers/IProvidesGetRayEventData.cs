using Unity.Labs.EditorXR.Modules;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Provide access to pointer event data
    /// </summary>
    interface IProvidesGetRayEventData : IFunctionalityProvider
    {
        RayEventData GetPointerEventData(Transform rayOrigin);
    }
}
