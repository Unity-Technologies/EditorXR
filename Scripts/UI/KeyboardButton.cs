using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
	private bool m_Holding;

	private Action<char> m_KeyPress;

	private Func<bool> m_PressOnHover;

	public void Setup(Action<char> keyPress, Func<bool> pressOnHover)
	{
		m_PressOnHover = pressOnHover;

		m_KeyPress = keyPress;
	}

	public void SetShiftModeActive(bool active)
	{
		if (!m_UseShiftCharacter) return;

		m_ShiftMode = active;

		if (m_TextComponent != null && m_MatchButtonTextToCharacter)
		{
			if (m_ShiftMode)
			{
				if (m_ShiftCharIsUppercase)
					m_TextComponent.text = m_TextComponent.text.ToUpper();
				else if (m_ShiftCharacter != 0)
					m_TextComponent.text = m_ShiftCharacter.ToString();
			}
			else
				m_TextComponent.text = m_Character.ToString();

			m_TextComponent.enabled = false;
			m_TextComponent.enabled = true;
		}
	}

	public override void OnPointerClick(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerClick(eventData);
			NumericKeyPressed();
		}
	}

	public void OnBeginDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold && !m_PressOnHover())
			StartHold();
	}

	public void OnDrag(RayEventData eventData)
	{
		if (U.UI.IsValidEvent(eventData, selectionFlags) && m_RepeatOnHold)
			Hold();
	}

	public void NumericKeyPressed()
	{
		if (m_KeyPress == null) return;

		if (m_ShiftMode && !m_ShiftCharIsUppercase && m_ShiftCharacter != 0)
			m_KeyPress(m_ShiftCharacter);
		else
			m_KeyPress(m_Character);
	}

	public void OnTriggerEnter(Collider col)
	{
		if (!m_PressOnHover() || col.GetComponentInParent<KeyboardMallet>() == null)
			return;

		NumericKeyPressed();

		if (m_RepeatOnHold)
			StartHold();
	}

	public void OnTriggerStay(Collider col)
	{
		if (col.GetComponentInParent<KeyboardMallet>() == null) return;

		if (m_Holding && m_RepeatOnHold)
			Hold();
	}

	public void OnTriggerExit(Collider col)
	{
		if (col.GetComponentInParent<KeyboardMallet>() == null) return;

		EndHold();
	}

	private void StartHold()
	{
		m_Holding = true;
		m_HoldStartTime = Time.realtimeSinceStartup;
		m_RepeatWaitTime = m_RepeatTime;
	}

	private void Hold()
	{
		if (m_Holding && m_HoldStartTime + m_RepeatWaitTime < Time.realtimeSinceStartup)
		{
			NumericKeyPressed();
			m_HoldStartTime = Time.realtimeSinceStartup;
			m_RepeatWaitTime *= 0.75f;
		}
	}

	private void EndHold()
	{
		m_Holding = false;
	}
}
