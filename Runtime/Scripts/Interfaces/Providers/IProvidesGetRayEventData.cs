using Unity.EditorXR.Modules;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR
{
    /// <summary>
    /// Provide access to pointer event data
    /// </summary>
    interface IProvidesGetRayEventData : IFunctionalityProvider
    {
        RayEventData GetPointerEventData(Transform rayOrigin);
    }
}
