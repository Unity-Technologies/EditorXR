using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
public class KeyboardUI : MonoBehaviour
{
	public List<KeyboardButton> buttons { get { return m_Buttons; } set { m_Buttons = value; } }
	[SerializeField]
	private List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	private Canvas m_Canvas;

	public void Setup(Action<char> keyPress, bool pressOnHover = false)
	{
		foreach (var button in m_Buttons)
		{
			button.Setup(keyPress, pressOnHover);
		}
	}

	public void SetKeyTextToUpperCase()
	{
		foreach (var button in m_Buttons)
		{
			if (button.textComponent != null)
			{
				button.textComponent.text = button.textComponent.text.ToUpper();
				button.textComponent.enabled = false;
				button.textComponent.enabled = true;
			}
		}

	}

	public void SetKeyTextToLowerCase()
	{
		foreach (var button in m_Buttons)
		{
			if (button.textComponent != null)
			{
				button.textComponent.text = button.textComponent.text.ToLower();
				button.textComponent.enabled = false;
				button.textComponent.enabled = true;
			}
		}
	}

	private void Update()
	{
//		if (Vector3.Dot(transform.up, Vector3.up) > 0.5f))

	}
}
