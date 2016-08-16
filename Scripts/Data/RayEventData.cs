using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.VR.Modules
{
	public class RayEventData : PointerEventData
	{
		public Transform rayOrigin { get; set; }

		public RayEventData(EventSystem eventSystem) : base(eventSystem)
		{
		}
	}
}