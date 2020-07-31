using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayHover events
    /// </summary>
    interface IRayHoverHandler : IEventSystemHandler
    {
        void OnRayHover(RayEventData eventData);
    }
}
