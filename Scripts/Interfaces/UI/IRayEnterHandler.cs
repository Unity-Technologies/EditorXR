using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayEnter events
	/// </summary>
	public interface IRayEnterHandler : IEventSystemHandler
	{
		void OnRayEnter(RayEventData eventData);
	}
}