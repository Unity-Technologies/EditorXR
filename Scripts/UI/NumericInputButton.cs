using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

/// <summary>
/// Set either the button's text field or the ASCII value
/// </summary>
public class NumericInputButton : RayButton
{
	public enum CharacterDescriptionType
	{
		Character,
		Special,
	}
	[SerializeField]
	private CharacterDescriptionType m_CharacterDescriptionType = CharacterDescriptionType.Character;

	public enum SpecialKeyType
	{
		None,
		Backspace = 8,
		Return = 13,
		Space = 32,
		Clear = 127,
	}
	[SerializeField]
	private SpecialKeyType m_SpecialKeyType = SpecialKeyType.None;

	[SerializeField]
	private char m_KeyCode;

	[SerializeField]
	private Text m_TextComponent;

	[SerializeField]
	private bool m_MatchButtonTextToCharacter = true;

	[SerializeField]
	private GameObject m_ButtonMesh;

	[SerializeField]
	private bool m_RepeatOnHold;

	[SerializeField]
	private float m_RepeatTime = 0.5f;

	private float m_HoldStartTime;
	private bool m_ButtonDown;

	private Action<char> m_KeyPress;

	private UnityEvent m_Trigger = new UnityEvent();

	public void Setup(Action<char> keyPress, bool pressOnHover)
	{
		m_KeyPress = keyPress;

		if (m_CharacterDescriptionType ==  CharacterDescriptionType.Special)
			m_KeyCode = (char) m_SpecialKeyType;

		if (m_CharacterDescriptionType == CharacterDescriptionType.Character && m_MatchButtonTextToCharacter)
		{
			if (m_TextComponent != null)
				m_TextComponent.text = m_KeyCode.ToString();
		}

		m_Trigger = pressOnHover ? (UnityEvent) onEnter : onDown;

		m_Trigger.AddListener(NumericKeyPressed);
	}

	protected override void OnDisable()
	{
		m_Trigger.RemoveListener(NumericKeyPressed);

		base.OnDisable();
	}

	private void NumericKeyPressed()
	{
		m_KeyPress(m_KeyCode);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerDown(eventData);

			m_ButtonDown = true;
			if (m_RepeatOnHold)
				StartCoroutine(RepeatPress());
		}
	}

	private IEnumerator RepeatPress()
	{
		m_HoldStartTime = Time.realtimeSinceStartup;
		var repeatWaitTime = m_RepeatTime;

		while (m_ButtonDown)
		{
			if (m_HoldStartTime + repeatWaitTime < Time.realtimeSinceStartup)
			{
				NumericKeyPressed();
				m_HoldStartTime = Time.realtimeSinceStartup;
				repeatWaitTime *= 0.75f;
			}

			yield return null;
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		var rayEventData = eventData as RayEventData;
		if (rayEventData == null || U.UI.IsValidEvent(rayEventData, selectionFlags))
		{
			base.OnPointerUp(eventData);

			m_ButtonDown = false;
		}
	}
}
