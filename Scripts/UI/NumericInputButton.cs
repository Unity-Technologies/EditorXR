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

		m_Trigger = pressOnHover ? (UnityEvent) onEnter : onClick;

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
}
