using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayExit events
	/// </summary>
	public interface IRayExitHandler : IEventSystemHandler
	{
		void OnRayExit(RayEventData eventData);
	}
}