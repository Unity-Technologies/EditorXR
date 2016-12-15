using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayEnter events
	/// </summary>
	public interface IRayEnterHandler : IEventSystemHandler
	{
		void OnRayEnter(RayEventData eventData);
	}
}