using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

public class NumericInputButton : RayButton
{
	[SerializeField]
	private GameObject m_ButtonMesh;
	
	[SerializeField]
	private Text m_ButtonText;

	private Action<char> m_KeyPress;

	private char m_KeyChar;

	private bool m_RequireClick;

	public void Setup(char keyChar, Action<char> keyPress, bool pressOnHover)
	{
		m_KeyChar = keyChar;
		m_KeyPress = keyPress;
		m_RequireClick = !pressOnHover;

		if (m_ButtonText != null)
			m_ButtonText.text = keyChar.ToString();

		UnityEvent trigger;

		if (m_RequireClick)
		{
			trigger = onClick;
//			onClick.AddListener(NumericKeyPressed);
		}
		else
		{
			trigger = onEnter;
//			onEnter.AddListener(NumericKeyPressed);
		}

		if (char.IsNumber(keyChar))
		{
			trigger.AddListener(NumericKeyPressed);
		}
		else
		{
			switch (keyChar)
			{
				case 'r':
					trigger.AddListener(SubmitButtonPressed);
					break;
				case '*':
					trigger.AddListener(MultiplyButtonPressed);
					break;
				case '/':
					trigger.AddListener(DivideButtonPressed);
					break;
			}
		}
	}

	protected override void OnDisable()
	{
		onClick.RemoveListener(NumericKeyPressed);
		onEnter.RemoveListener(NumericKeyPressed);

		base.OnDisable();
	}

	private void NumericKeyPressed()
	{
		m_KeyPress(m_KeyChar);
	}

	private void SubmitButtonPressed()
	{
		
	}

	private void DivideButtonPressed()
	{
		
	}

	private void MultiplyButtonPressed()
	{
		
	}

	/*
	public override void OnPointerEnter(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerEnter(eventData);

			if (!m_RequireClick)
			{
				m_KeyPress(m_KeyChar);
			}
		}
	}
	*/

	/*
	protected override void OnHandleBeginDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData(transform, true));
	}

	protected override void OnHandleDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData( transform, true ));
	}

	protected override void OnHandleEndDrag(HandleEventData eventData)
	{
		// Prevent button from being moved by tool
		base.OnHandleBeginDrag(new HandleEventData(transform, true));
	}
	*/
}
