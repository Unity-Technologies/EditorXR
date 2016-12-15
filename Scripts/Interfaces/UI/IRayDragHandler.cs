using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayDrag events
	/// </summary>
	public interface IRayDragHandler : IEventSystemHandler
	{
		void OnDrag(RayEventData eventData);
	}
}