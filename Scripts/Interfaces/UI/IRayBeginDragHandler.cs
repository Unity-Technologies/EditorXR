using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayBeginDrag events
	/// </summary>
	public interface IRayBeginDragHandler : IEventSystemHandler
	{
		void OnBeginDrag(RayEventData eventData);
	}
}