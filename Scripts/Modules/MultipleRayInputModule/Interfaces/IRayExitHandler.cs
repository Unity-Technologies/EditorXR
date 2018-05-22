
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayExit events
    /// </summary>
    interface IRayExitHandler : IEventSystemHandler
    {
        void OnRayExit(RayEventData eventData);
    }
}

