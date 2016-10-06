using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayBeginDragHandler : IEventSystemHandler
	{
		void OnBeginDrag(RayEventData eventData);
	}
}