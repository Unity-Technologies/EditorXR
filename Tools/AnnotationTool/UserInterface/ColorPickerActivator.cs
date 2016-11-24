using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityEngine.VR.Tools;
using System.Collections.Generic;

public class ColorPickerActivator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IUsesRayOrigin
{

	public Transform rayOrigin { private get; set; }

	[SerializeField]
	Transform m_Icon;

	Coroutine m_Coroutine;
	
	public Action<Transform> showColorPicker { private get; set; }
	
	public void OnPointerClick(PointerEventData eventData)
	{
		if (showColorPicker != null)
			showColorPicker(rayOrigin);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (m_Coroutine != null)
			StopCoroutine(m_Coroutine);

		m_Coroutine = StartCoroutine(Highlight());

		if (showColorPicker != null)
			showColorPicker(rayOrigin);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_Coroutine != null)
			StopCoroutine(m_Coroutine);

		m_Coroutine = StartCoroutine(Highlight(false));
	}

	IEnumerator Highlight(bool transitionIn = true)
	{
		var amount = 0f;
		var currentScale = m_Icon.localScale;
		var targetScale = transitionIn == true ? Vector3.one * 1.5f : Vector3.one;
		var speed = (currentScale.x + 0.5f / targetScale.x) * 4;

		while (amount < 1f)
		{
			amount += Time.unscaledDeltaTime * speed;
			m_Icon.localScale = Vector3.Lerp(currentScale, targetScale, Mathf.SmoothStep(0f, 1f, amount));
			yield return null;
		}

		m_Icon.localScale = targetScale;
	}

}
