using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Handles
{
	public class DirectHandle : SphereHandle, IRayDragHandler
	{
		[SerializeField]
		private float m_PointerLength = .06f;

		public override void OnBeginDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerCurrentRaycast.distance > m_PointerLength)
				return;
			base.OnBeginDrag(eventData);
		}

		public new void OnDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
				return;
		}

		public override void OnEndDrag(RayEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
				return;
			base.OnEndDrag(eventData);
		}
	}
}