using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayBeginDrag events
    /// </summary>
    interface IRayBeginDragHandler : IEventSystemHandler
    {
        void OnBeginDrag(RayEventData eventData);
    }
}
