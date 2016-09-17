using System;
using UnityEngine;
using System.Collections.Generic;

public class NumericKeyboardUI : MonoBehaviour
{
	private List<NumericInputButton> m_Buttons = new List<NumericInputButton>();

	private void Awake()
	{
		foreach (var button in GetComponentsInChildren<NumericInputButton>())
		{
			m_Buttons.Add(button);
		}
	}

//	public void Setup(char[] keyChars, Action<char> keyPress, bool pressOnHover = false)
	public void Setup(Action<char> keyPress, bool pressOnHover = false)
	{
		foreach (var button in GetComponentsInChildren<NumericInputButton>())
		{
			button.Setup(keyPress, pressOnHover);
		}
	}
}
