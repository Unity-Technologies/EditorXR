using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayHoverHandler : IEventSystemHandler
	{
		void OnRayHover(RayEventData eventData);
	}
}