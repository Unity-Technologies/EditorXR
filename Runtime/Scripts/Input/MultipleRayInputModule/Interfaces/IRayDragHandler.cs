using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayDrag events
    /// </summary>
    interface IRayDragHandler : IEventSystemHandler
    {
        void OnDrag(RayEventData eventData);
    }
}
