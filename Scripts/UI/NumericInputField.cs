using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputField : InputField
{
	public SelectionFlags selectionFlags { get { return m_SelectionFlags; } set { m_SelectionFlags = value; } }
	[SerializeField]
	[FlagsProperty]
	private SelectionFlags m_SelectionFlags = SelectionFlags.Ray | SelectionFlags.Direct;

	public Func<NumericKeyboardUI> keyboard;
	private NumericKeyboardUI m_Keyboard;

	private string m_String;
	private bool m_Open;

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerClick(eventData);

			if (m_Open)
				Close();
			else
				Open();
		}
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
		{
			base.OnSubmit(eventData);

			Close();
		}
	}

	public override void OnSelect(BaseEventData eventData)
	{
		//
	}

	void Open()
	{
		if (m_Open) return;

		m_Open = true;

		m_String = text;

		m_Keyboard = keyboard();
		// Instantiate keyboard here
		if (m_Keyboard != null)
		{
			m_Keyboard.gameObject.SetActive(true);
			m_Keyboard.transform.SetParent(transform, false);
			m_Keyboard.transform.localPosition = Vector3.up * 0.1f;
			m_Keyboard.transform.localRotation = Quaternion.identity;

			m_Keyboard.Setup(new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'}, OnKeyPress);
		}
	}

	void Close()
	{
		m_Open = false;

		m_Keyboard.gameObject.SetActive(true);
		m_Keyboard = null;
	}

	void OnKeyPress(char keyChar)
	{
		m_String += keyChar;
		text = m_String;
	}
}
