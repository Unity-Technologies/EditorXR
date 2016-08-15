using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.VR.Handles
{
	public class DirectHandle : SphereHandle
	{
		[SerializeField] private float m_PointerLength = .06f;

		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerCurrentRaycast.distance > m_PointerLength)
				return;
			base.OnBeginDrag(eventData);
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
				return;
		}

		public override void OnEndDrag(PointerEventData eventData)
		{
			if (!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
				return;
			base.OnEndDrag(eventData);
		}
	}
}