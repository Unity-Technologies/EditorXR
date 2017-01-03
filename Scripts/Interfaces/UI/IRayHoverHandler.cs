using UnityEngine.EventSystems;

namespace UnityEngine.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayHover events
	/// </summary>
	public interface IRayHoverHandler : IEventSystemHandler
	{
		void OnRayHover(RayEventData eventData);
	}
}