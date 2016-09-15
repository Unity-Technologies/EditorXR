using System;
using UnityEngine;
using System.Collections.Generic;

public class NumericKeyboardUI : MonoBehaviour
{
	private List<NumericInputButton> m_Buttons = new List<NumericInputButton>();

//	public void Setup(char[] keyChars, Action<char> keyPress, bool pressOnHover = false)
	public void Setup(Action<char> keyPress, bool pressOnHover = false)
	{
		foreach (var button in GetComponentsInChildren<NumericInputButton>())
		{
			m_Buttons.Add(button);
			button.Setup(keyPress, pressOnHover);
		}
	}
}