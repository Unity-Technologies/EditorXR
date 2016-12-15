using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayDrag events
	/// </summary>
	public interface IRayDragHandler : IEventSystemHandler
	{
		void OnDrag(RayEventData eventData);
	}
}