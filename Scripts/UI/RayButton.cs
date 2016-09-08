using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class RayButton : Button {
	public HandleFlags handleFlags { get { return m_HandleFlags; } set { m_HandleFlags = value; } }
	[SerializeField]
	[FlagsProperty]
	private HandleFlags m_HandleFlags = HandleFlags.Ray | HandleFlags.Direct;

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if(rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnPointerClick(eventData);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnPointerExit(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnPointerDown(eventData);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnPointerUp(eventData);
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		Debug.Log(rayEventData);
		if (rayEventData == null || U.Input.IsValidEvent(rayEventData, handleFlags))
			base.OnSubmit(eventData);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//Not selectable
	}
}