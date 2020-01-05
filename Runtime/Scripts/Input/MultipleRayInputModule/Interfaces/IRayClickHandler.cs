using UnityEngine.EventSystems;

namespace Unity.Labs.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayClick events
    /// </summary>
    interface IRayClickHandler : IEventSystemHandler
    {
        void OnRayClick(RayEventData eventData);
    }
}
