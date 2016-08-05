using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Proxies;

public class DirectHandle : SphereHandle
{
	[SerializeField]
	private float m_PointerLength = .06f;

	public override void OnBeginDrag(PointerEventData eventData)
	{
		if(!eventData.pointerPressRaycast.isValid || eventData.pointerCurrentRaycast.distance > m_PointerLength)
			return;
		base.OnBeginDrag(eventData);
	}

	public override void OnDrag(PointerEventData eventData)
	{
		if(!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
			return;
		base.OnDrag(eventData);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		if(!eventData.pointerPressRaycast.isValid || eventData.pointerPressRaycast.distance > m_PointerLength)
			return;
		base.OnEndDrag(eventData);
	}
}
