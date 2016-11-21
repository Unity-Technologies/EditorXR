using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerSquareUI : Selectable, IDragHandler, IBeginDragHandler, IEndDragHandler
{

	private bool m_AllowDragEvents;
	public Action onDrag { private get; set; }

	public void OnBeginDrag(PointerEventData eventData)
	{
		m_AllowDragEvents = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (onDrag != null && m_AllowDragEvents)
			onDrag();
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		m_AllowDragEvents = false;
	}

}
