using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class DirectHandle : SphereHandle, IRayDragHandler
	{
		public override void OnBeginDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerCurrentRaycast.distance > eventData.pointerLength)
				return;
			base.OnBeginDrag(eventData);
		}

		public new void OnDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > eventData.pointerLength)
				return;
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > eventData.pointerLength)
				return;
			base.OnEndDrag(eventData);
		}
	}
}