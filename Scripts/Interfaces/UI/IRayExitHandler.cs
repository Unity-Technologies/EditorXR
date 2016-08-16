using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayExitHandler : IEventSystemHandler
	{
		void OnRayExit(RayEventData eventData);
	}
}