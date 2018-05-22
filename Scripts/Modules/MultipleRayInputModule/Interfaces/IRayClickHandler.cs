
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayClick events
    /// </summary>
    interface IRayClickHandler : IEventSystemHandler
    {
        void OnRayClick(RayEventData eventData);
    }
}

