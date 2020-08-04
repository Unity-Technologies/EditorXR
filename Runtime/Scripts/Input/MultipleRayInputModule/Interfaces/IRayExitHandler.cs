using UnityEngine.EventSystems;

namespace Unity.EditorXR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayExit events
    /// </summary>
    interface IRayExitHandler : IEventSystemHandler
    {
        void OnRayExit(RayEventData eventData);
    }
}
