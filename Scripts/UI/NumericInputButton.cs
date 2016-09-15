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

	private UnityEvent m_Trigger = new UnityEvent();

	public void Setup(Action<char> keyPress, bool pressOnHover)
	{
//		m_KeyChar = keyChar;
		m_KeyPress = keyPress;

		if (m_ButtonText != null && m_ButtonText.text.Length > 0)
//			m_ButtonText.text = keyChar.ToString();
			m_KeyChar = m_ButtonText.text[0];

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
		m_KeyPress(m_KeyChar);
	}
}
