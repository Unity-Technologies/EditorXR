using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayBeginDrag events
    /// </summary>
    interface IRayPointerUpHandler : IEventSystemHandler
    {
        void OnPointerUp(RayEventData eventData);
    }
}
