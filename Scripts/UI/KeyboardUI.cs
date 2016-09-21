using System;
using UnityEngine;
using System.Collections.Generic;

public class KeyboardUI : MonoBehaviour
{
	public List<KeyboardButton> buttons { get { return m_Buttons; } set { m_Buttons = value; } }
	[SerializeField]
	private List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	public void Setup(Action<char> keyPress, bool pressOnHover = false)
	{
		foreach (var button in GetComponentsInChildren<KeyboardButton>())
		{
			button.Setup(keyPress, pressOnHover);
		}
	}
}
