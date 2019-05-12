using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayBeginDrag events
    /// </summary>
    interface IRayBeginDragHandler : IEventSystemHandler
    {
        void OnBeginDrag(RayEventData eventData);
    }
}
