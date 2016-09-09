using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class RayButton : Button {
	public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
	[SerializeField]
	[FlagsProperty]
	private SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if(rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerClick(eventData);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerExit(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerDown(eventData);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnPointerUp(eventData);
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		Debug.Log(rayEventData);
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
			base.OnSubmit(eventData);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//Not selectable
	}
}