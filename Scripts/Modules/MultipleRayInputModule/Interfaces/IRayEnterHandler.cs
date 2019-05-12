using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Decorates objects with functionality to detect RayEnter events
    /// </summary>
    interface IRayEnterHandler : IEventSystemHandler
    {
        void OnRayEnter(RayEventData eventData);
    }
}
