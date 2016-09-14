using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NumericInputButton : RayButton
{
	[SerializeField]
	private GameObject m_ButtonMesh;
	
	[SerializeField]
	private Text m_ButtonText;

	private Action<char> m_KeyPress;

	private char m_KeyChar;

	private bool m_RequireClick;

	private UnityEvent m_Trigger;

	public void Setup(char keyChar, Action<char> keyPress, bool pressOnHover)
	{
		m_KeyChar = keyChar;
		m_KeyPress = keyPress;
		m_RequireClick = !pressOnHover;

		if (m_ButtonText != null)
			m_ButtonText.text = keyChar.ToString();

		if (m_RequireClick)
			m_Trigger = onClick;
		else
			m_Trigger = onEnter;

		m_Trigger.AddListener(NumericKeyPressed);
	}

	protected override void OnDisable()
	{
		m_Trigger.RemoveListener(NumericKeyPressed);

		base.OnDisable();
	}

	private void NumericKeyPressed()
	{
		m_KeyPress(m_KeyChar);
	}
}
