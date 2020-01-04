using UnityEngine.EventSystems;

namespace Unity.Labs.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayEndDrag events
    /// </summary>
    interface IRayEndDragHandler : IEventSystemHandler
    {
        void OnEndDrag(RayEventData eventData);
    }
}
