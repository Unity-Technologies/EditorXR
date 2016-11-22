using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityEngine.VR.Tools;

public class ColorPickerActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IMenuOrigins, IUsesRayOrigin
{
	public Transform alternateMenuOrigin { private get; set; }
	public Transform menuOrigin { private get; set; }

	public Transform rayOrigin { private get; set; }

	public void OnPointerClick(PointerEventData eventData)
	{

	}

	public void OnPointerEnter(PointerEventData eventData)
	{

	}

	public void OnPointerExit(PointerEventData eventData)
	{

	}

}
