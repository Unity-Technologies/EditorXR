using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
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
