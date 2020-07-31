using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayEndDrag events
    /// </summary>
    interface IRayEndDragHandler : IEventSystemHandler
    {
        void OnEndDrag(RayEventData eventData);
    }
}
