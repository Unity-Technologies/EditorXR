using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayEnterHandler : IEventSystemHandler
	{
		void OnRayEnter(RayEventData eventData);
	}
}