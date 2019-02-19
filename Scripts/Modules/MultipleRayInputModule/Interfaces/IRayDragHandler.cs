using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayDrag events
    /// </summary>
    interface IRayDragHandler : IEventSystemHandler
    {
        void OnDrag(RayEventData eventData);
    }
}
