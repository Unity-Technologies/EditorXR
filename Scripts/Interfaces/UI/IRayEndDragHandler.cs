using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayEndDrag events
	/// </summary>
	public interface IRayEndDragHandler : IEventSystemHandler
	{
		void OnEndDrag(RayEventData eventData);
	}
}