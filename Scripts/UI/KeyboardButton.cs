using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

/// <summary>
/// Set either the button's text field or the ASCII value
/// </summary>
public class KeyboardButton : RayButton, IRayBeginDragHandler, IRayDragHandler
{
	public Text textComponent { get { return m_TextComponent; } set { m_TextComponent = value; } }
	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private char m_Character;

	[SerializeField]
	private bool m_UseShiftCharacter;

	[SerializeField]
	private char m_ShiftCharacter;

	[SerializeField]
	private bool m_ShiftCharIsUppercase;

	private bool m_ShiftMode;

	[SerializeField]
	private bool m_MatchButtonTextToCharacter;

	[SerializeField]
	private Image m_ButtonIcon;

	[SerializeField]
	private GameObject m_ButtonMesh;

	[SerializeField]
	private bool m_RepeatOnHold;

	[SerializeField]
	private float m_RepeatTime = 0.5f;

	private float m_HoldStartTime;
	private float m_RepeatWaitTime;

	private Action<char> m_KeyPress;

	private UnityEvent m_Trigger = new UnityEvent();

	public void Setup(Action<char> keyPress, bool pressOnHover)
	{
		m_Trigger.RemoveAllListeners();

		m_KeyPress = keyPress;

		m_Trigger = pressOnHover ? onEnter : onDown;

		m_Trigger.AddListener(NumericKeyPressed);
	}

	public void SetShiftModeActive(bool active)
	{
		if (!m_UseShiftCharacter || m_ShiftCharacter == 0) return;

		m_ShiftMode = active;

		if (m_TextComponent != null && m_MatchButtonTextToCharacter)
		{
			if (m_ShiftMode)
			{
				if (m_ShiftCharIsUppercase)
					m_TextComponent.text = m_TextComponent.text.ToUpper();
				else
					m_TextComponent.text = m_ShiftCharacter.ToString();
			}
			else
				m_TextComponent.text = m_Character.ToString();

			m_TextComponent.enabled = false;
			m_TextComponent.enabled = true;
		}
	}

	public void OnBeginDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold)
		{
			m_HoldStartTime = Time.realtimeSinceStartup;
			m_RepeatWaitTime = m_RepeatTime;
		}
	}

	public void OnDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold)
		{
			if (m_HoldStartTime + m_RepeatWaitTime < Time.realtimeSinceStartup)
			{
				NumericKeyPressed();
				m_HoldStartTime = Time.realtimeSinceStartup;
				m_RepeatWaitTime *= 0.75f;
			}
		}
	}

	protected override void OnDisable()
	{
		m_Trigger.RemoveListener(NumericKeyPressed);

		base.OnDisable();
	}

	protected void NumericKeyPressed()
	{
		if (m_KeyPress == null) return;

		if (m_ShiftMode && !m_ShiftCharIsUppercase)
			m_KeyPress(m_ShiftCharacter);
		else
			m_KeyPress(m_Character);
	}

}
