using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	/// <summary>
	/// Decorates objects with functionality to detect RayDrag events
	/// </summary>
	internal interface IRayDragHandler : IEventSystemHandler
	{
		void OnDrag(RayEventData eventData);
	}
}