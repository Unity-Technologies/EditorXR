using System;
using UnityEngine;
using System.Collections.Generic;

public class NumericKeyboardUI : MonoBehaviour
{
	public List<NumericInputButton> buttons { get { return m_Buttons; } set { m_Buttons = value; } }
	[SerializeField]
	private List<NumericInputButton> m_Buttons = new List<NumericInputButton>();

	public void Setup(Action<char> keyPress, bool pressOnHover = false)
	{
		foreach (var button in GetComponentsInChildren<NumericInputButton>())
		{
			button.Setup(keyPress, pressOnHover);
		}
	}
}
