using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : RayButton, IRayBeginDragHandler, IRayDragHandler
{
//	public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
//	[SerializeField]
//	[FlagsProperty]
//	protected SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_NumericKeyboard;

	[SerializeField]
	private Text m_TextComponent;
	private string m_String;

	private bool m_Open;

	private bool m_PointerOverField;
	private Vector3 m_LastPointerHitPosition;


	//	private float m_TimeCounter;
	private float m_ClickThresholdTime = 0.3f;
	private float m_PressedTime;

	public void SetText(string text)
	{
		var isValidString = true;

		foreach (var ch in text)
		{
			isValidString = isValidString && IsValidCharacter(ch);
		}

		if (isValidString)
		{
			m_TextComponent.text = m_String = text;
		}
	}

	public void UpdateInspectorFieldText()
	{
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerClick(eventData);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerEnter(eventData);

			m_PointerOverField = true;

			if (eventData.dragging)
				m_LastPointerHitPosition = GetCurrentRayHitPosition(rayEventData);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerExit(eventData);

			m_PointerOverField = false;
		}
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerDown(eventData);

			m_PressedTime = Time.realtimeSinceStartup;
		}
	}

//	private IEnumerator TrackClickTime(PointerEventData eventData)
//	{
//		while (m_TimeCounter < m_ClickThresholdTime)
//		{
//			m_TimeCounter += Time.unscaledDeltaTime;
//			yield return null;
//		}
//
//		eventData.eligibleForClick = false;
//	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerUp(eventData);

			if (Time.realtimeSinceStartup - m_PressedTime < m_ClickThresholdTime)
			{
				if (m_Open)
					Close();
				else
					Open();
			}
		}
	}

	public override void OnSubmit(BaseEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnSubmit(eventData);
		}
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		// TODO this works but need to only deselect when something besides a key button is clicked
//	    Debug.Log("Deselect callled");
//		Close();
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//
	}

	private void Open()
	{
		if (m_Open) return;
		m_Open = true;

		m_NumericKeyboard = keyboard();
		// Instantiate keyboard here
		if (m_NumericKeyboard != null)
		{
			m_NumericKeyboard.gameObject.SetActive(true);
			m_NumericKeyboard.transform.SetParent(transform, true);
			m_NumericKeyboard.transform.localPosition = Vector3.up * 0.2f;
			m_NumericKeyboard.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

//			m_NumericKeyboard.Setup(new char[] {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.'}, OnKeyPress);
			m_NumericKeyboard.Setup(OnKeyPress);
		}
	}

	private void Close()
	{
		m_Open = false;

		if (m_NumericKeyboard == null) return;

		m_NumericKeyboard.gameObject.SetActive(false);
		m_NumericKeyboard = null;
	}

	private void OnKeyPress(char keyChar)
	{
		if (IsValidCharacter(keyChar))
			m_TextComponent.text = m_String += keyChar;

		//TODO handle delete and multiplication and whatnot here
	}

	bool IsValidCharacter(char ch)
	{
		if (ch >= '0' && ch <= '9') return true;
		if (ch == '-' && (m_String.Length == 0)) return true;
		if (ch == '.' && !m_String.Contains(".")) return true;

		return false;
	}

	public void OnBeginDrag( RayEventData eventData )
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags))
			return;

		m_LastPointerHitPosition = GetCurrentRayHitPosition(eventData);
	}

	public void OnDrag( RayEventData eventData )
	{
		if (!U.UI.IsValidEvent(eventData, selectionFlags))
			return;

		if (m_PointerOverField)
		{
			DragNumericValue(eventData);
			m_LastPointerHitPosition = GetCurrentRayHitPosition(eventData);
		}
	}

	void DragNumericValue(RayEventData eventData)
	{
		float num;
		if (!float.TryParse(m_TextComponent.text, out num))
			num = 0f;

		var xDelta =
			(transform.InverseTransformPoint(GetCurrentRayHitPosition(eventData)) -
			 transform.InverseTransformPoint(m_LastPointerHitPosition)).x;

		num += xDelta * 10f;

		m_String = num.ToString();
		m_TextComponent.text = m_String;
	}

	Vector3 GetCurrentRayHitPosition(RayEventData eventData)
	{
		return eventData.rayOrigin.position + eventData.rayOrigin.forward * eventData.pointerCurrentRaycast.distance;
	}
}
