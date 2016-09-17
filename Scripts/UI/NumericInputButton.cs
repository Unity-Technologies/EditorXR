using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
	private CharacterDescriptionType m_CharacterDescriptionType;

	public enum SpecialKeyType
	{
		Backspace,
		Return,
		Clear,
	}
	[SerializeField]
	private SpecialKeyType m_SpecialKeyType;

	[SerializeField]
	private char m_KeyCode;

	[SerializeField]
	private GameObject m_ButtonMesh;

	[SerializeField]
	private bool m_MatchButtonTextToCharacter = true;
	[SerializeField]
	private Text m_TextComponent;

	private Action<char> m_KeyPress;

	private UnityEvent m_Trigger = new UnityEvent();

	public void Setup(Action<char> keyPress, bool pressOnHover)
	{
		m_KeyPress = keyPress;

		switch (m_CharacterDescriptionType)
		{
			case CharacterDescriptionType.Special:
				if (m_SpecialKeyType == SpecialKeyType.Backspace)
					m_KeyCode = 'b';
				else if (m_SpecialKeyType == SpecialKeyType.Clear)
					m_KeyCode = 'c';
				else if (m_SpecialKeyType == SpecialKeyType.Return)
					m_KeyCode = 'r';
				break;
			case CharacterDescriptionType.Character:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		if (m_MatchButtonTextToCharacter)
		{
			if (m_TextComponent != null)
				m_TextComponent.text = m_KeyCode.ToString();
		}

		if (pressOnHover)
			m_Trigger = onEnter;
		else
			m_Trigger = onClick;

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
