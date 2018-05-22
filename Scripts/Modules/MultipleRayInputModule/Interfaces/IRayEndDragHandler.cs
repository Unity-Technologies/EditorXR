
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayEndDrag events
    /// </summary>
    interface IRayEndDragHandler : IEventSystemHandler
    {
        void OnEndDrag(RayEventData eventData);
    }
}

