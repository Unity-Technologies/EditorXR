
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayHover events
    /// </summary>
    interface IRayHoverHandler : IEventSystemHandler
    {
        void OnRayHover(RayEventData eventData);
    }
}

