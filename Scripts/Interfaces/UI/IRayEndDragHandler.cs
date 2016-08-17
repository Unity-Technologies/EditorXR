using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{	
	public interface IRayEndDragHandler : IEventSystemHandler
	{
		void OnEndDrag(RayEventData eventData);
	}
}