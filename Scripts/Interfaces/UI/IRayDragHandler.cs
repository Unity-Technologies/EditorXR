using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayDragHandler : IEventSystemHandler
	{
		void OnDrag(RayEventData eventData);
	}
}