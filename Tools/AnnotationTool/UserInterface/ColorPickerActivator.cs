using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColorPickerActivator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

	[SerializeField]
	private Transform m_TargetScale;

	public Transform rayOrigin { private get; set; }
	public Action<Transform> showColorPicker { private get; set; }
	public Action hideColorPicker { private get; set; }

	[SerializeField]
	private Transform m_Icon;

	private Coroutine m_HighlightCoroutine;
	
	public void OnPointerClick(PointerEventData eventData)
	{
		eventData.Use();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (m_HighlightCoroutine != null)
			StopCoroutine(m_HighlightCoroutine);

		showColorPicker(rayOrigin);
		m_HighlightCoroutine = StartCoroutine(Highlight());
		
		eventData.Use();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_HighlightCoroutine != null)
			StopCoroutine(m_HighlightCoroutine);

		hideColorPicker();
		m_HighlightCoroutine = StartCoroutine(Highlight(false));
	}

	IEnumerator Highlight(bool transitionIn = true)
	{
		var amount = 0f;
		var currentScale = m_Icon.localScale;
		var targetScale = transitionIn ? m_TargetScale.localScale : Vector3.one;
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
