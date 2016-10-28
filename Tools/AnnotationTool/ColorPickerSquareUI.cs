using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerSquareUI : Selectable, IDragHandler
{

	public Action onDrag { private get; set; }

	public void OnDrag(PointerEventData eventData)
	{
		if (onDrag != null)
			onDrag();
	}

}
