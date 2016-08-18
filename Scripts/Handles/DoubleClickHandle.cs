using System;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;

public class DoubleClickHandle : BaseHandle {
	public event Action<BaseHandle> onDoubleClick;

	private const float kDoubleClickIntervalMax = 0.3f;
	private const float kDoubleClickIntervalMin = 0.15f;

	private DateTime m_LastClickTime;

	public override void OnBeginDrag(RayEventData eventData)
	{
		base.OnBeginDrag(eventData);
		var timeSinceLastClick = (float)(DateTime.Now - m_LastClickTime).TotalSeconds;
		m_LastClickTime = DateTime.Now;
		if (DoubleClick(timeSinceLastClick))
		{
			if (onDoubleClick != null)
				onDoubleClick(this);
		}
	}

	public static bool DoubleClick(float timeSinceLastClick)
	{
		return timeSinceLastClick < kDoubleClickIntervalMax && timeSinceLastClick > kDoubleClickIntervalMin;
	}
}