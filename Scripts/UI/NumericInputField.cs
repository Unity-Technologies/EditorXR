using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : RayButton, IRayBeginDragHandler, IRayDragHandler
{
	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_NumericKeyboard;

	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private float m_DragFactor = 10f;

	private string m_OutputString;
	private List<string> m_RawInputString = new List<string>();

	private bool m_Open;

	private bool m_PointerOverField;
	private Vector3 m_LastPointerHitPosition;

	private float m_ClickThresholdTime = 0.3f;
	private float m_PressedTime;

	public void SetTextFromInspectorField(string text)
	{
		var isValidString = true;

		foreach (var ch in text)
		{
			isValidString = isValidString && IsNumericCharacter(ch);
		}

		if (isValidString)
		{
			m_TextComponent.text = m_OutputString = text;
			m_RawInputString.Clear();
			m_RawInputString.Add(m_OutputString);
		}
	}

	/// <summary>
	/// Send string to the inspector field
	/// </summary>
	public void UpdateInspectorField()
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
		if (IsOperandCharacter(keyChar))
		{
			if (m_RawInputString.Count > 1)
				m_RawInputString.Add(keyChar.ToString());
		}
		else if (IsNumericCharacter(keyChar))
		{
			m_TextComponent.text = m_OutputString += keyChar;
		}
	}

	private bool IsNumericCharacter(char ch)
	{
		if (ch >= '0' && ch <= '9') return true;
		if (ch == '-' && (m_OutputString.Length == 0)) return true;
		if (ch == '.' && !m_OutputString.Contains(".")) return true;

		return false;
	}

	private bool IsOperandCharacter(char ch)
	{
		if (ch == '+' || ch == '-' || ch == '*' || ch == '/') return true;

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

	private void DragNumericValue(RayEventData eventData)
	{
		if (m_RawInputString.Count > 1) ProcessRawString();

		float num;
		if (!float.TryParse(m_TextComponent.text, out num))
			num = 0f;

		var xDelta =
			(transform.InverseTransformPoint(GetCurrentRayHitPosition(eventData)) -
			 transform.InverseTransformPoint(m_LastPointerHitPosition)).x;

		num += xDelta * 10f;

		m_OutputString = num.ToString();
		m_TextComponent.text = m_OutputString;

		UpdateInspectorField();
	}

	private void ProcessRawString()
	{
		for (int i = 1; i < m_RawInputString.Count; i++)
		{
//			if (
		}

		UpdateInspectorField();
	}

	private Vector3 GetCurrentRayHitPosition(RayEventData eventData)
	{
		var rayOriginPos = eventData.rayOrigin;
		return rayOriginPos.position + rayOriginPos.forward * eventData.pointerCurrentRaycast.distance;
	}
}
