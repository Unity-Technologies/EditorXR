using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayEnter events
    /// </summary>
    interface IRayEnterHandler : IEventSystemHandler
    {
        void OnRayEnter(RayEventData eventData);
    }
}
