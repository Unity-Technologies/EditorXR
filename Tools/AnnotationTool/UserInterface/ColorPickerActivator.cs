using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityEngine.VR.Tools;
using System.Collections.Generic;
using UnityEngine.VR.Menus;

public class ColorPickerActivator : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IUsesRayOrigin
{

	public Transform rayOrigin { private get; set; }

	[SerializeField]
	private Transform m_Icon;

	private Coroutine m_HighlightCoroutine;
	private Coroutine m_MoveCoroutine;

	public Action<Transform> showColorPicker { private get; set; }

	private RadialMenuUI m_RadialMenu;
	private Vector3 m_OriginalPosition;

	private bool m_ActivatorMoveAway;
	private bool activatorMoveAway
	{
		set
		{
			if (m_ActivatorMoveAway != value)
			{
				m_ActivatorMoveAway = value;

				if (m_MoveCoroutine != null)
					StopCoroutine(m_MoveCoroutine);

				m_MoveCoroutine = StartCoroutine(MoveAway(value));
			}
		}
	}

	void Start()
	{
		m_OriginalPosition = transform.localPosition;
		m_RadialMenu = transform.parent.GetComponentInChildren<RadialMenuUI>(true);
	}

	void Update()
	{
		if (m_RadialMenu)
			activatorMoveAway = m_RadialMenu.visible;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (showColorPicker != null)
			showColorPicker(rayOrigin);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (m_HighlightCoroutine != null)
			StopCoroutine(m_HighlightCoroutine);

		m_HighlightCoroutine = StartCoroutine(Highlight());

		if (showColorPicker != null)
			showColorPicker(rayOrigin);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_HighlightCoroutine != null)
			StopCoroutine(m_HighlightCoroutine);

		m_HighlightCoroutine = StartCoroutine(Highlight(false));
	}

	IEnumerator Highlight(bool transitionIn = true)
	{
		var amount = 0f;
		var currentScale = m_Icon.localScale;
		var targetScale = transitionIn ? Vector3.one * 1.5f : Vector3.one;
		var speed = (currentScale.x + 0.5f / targetScale.x) * 4;

		while (amount < 1f)
		{
			amount += Time.unscaledDeltaTime * speed;
			m_Icon.localScale = Vector3.Lerp(currentScale, targetScale, Mathf.SmoothStep(0f, 1f, amount));
			yield return null;
		}

		m_Icon.localScale = targetScale;
	}

	IEnumerator MoveAway(bool moveDown)
	{
		var amount = 0f;
		var currentPosition = transform.localPosition;
		var targetPosition = moveDown ? m_OriginalPosition + Vector3.back * 0.055f : m_OriginalPosition;
		var speed = (currentPosition.z / targetPosition.z) * (moveDown ? 10 : 3);

		while (amount < 1f)
		{
			amount += Time.unscaledDeltaTime * speed;
			transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.SmoothStep(0f, 1f, amount));
			yield return null;
		}

		transform.localPosition = targetPosition;
		m_MoveCoroutine = null;
	}

}
