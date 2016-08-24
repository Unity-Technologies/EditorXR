using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	//NOTE: Handles must be in the UI layer to receive events
	public class DirectHandle : SphereHandle, IRayDragHandler
	{
		private bool m_Hover;

		public override void OnRayEnter(RayEventData eventData) {
			if (eventData.pointerCurrentRaycast.distance > eventData.pointerLength)
				return;
			m_Hover = true;
			base.OnRayEnter(eventData);
		}

		public override void OnRayHover(RayEventData eventData) {
			if (eventData.pointerCurrentRaycast.distance > eventData.pointerLength) {
				if (m_Hover) {
					m_Hover = false;
					base.OnRayExit(eventData);
				}
				return;
			}
			if (!m_Hover) {
				m_Hover = true;
				base.OnRayEnter(eventData);
			}
			base.OnRayHover(eventData);
		}

		public override void OnRayExit(RayEventData eventData) {
			if (m_Hover)
				base.OnRayExit(eventData);
			m_Hover = false;
		}
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
			base.OnDrag(eventData);
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > eventData.pointerLength)
				return;
			base.OnEndDrag(eventData);
		}
	}
}