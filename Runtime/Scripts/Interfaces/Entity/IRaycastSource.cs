using Unity.Labs.EditorXR.Modules;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Declares a class as being a source for the MultipleRayInputModule
    /// </summary>
    interface IRaycastSource
    {
        RayEventData eventData { get; }
        bool hasObject { get; }
        bool blocked { get; set; }
        Transform rayOrigin { get; }
    }
}
