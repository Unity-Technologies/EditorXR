using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayBeginDrag events
    /// </summary>
    interface IRayPointerDownHandler : IEventSystemHandler
    {
        void OnPointerDown(RayEventData eventData);
    }
}
