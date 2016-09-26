using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
public class KeyboardUI : MonoBehaviour
{
	public List<KeyboardButton> buttons { get { return m_Buttons; } set { m_Buttons = value; } }
	[SerializeField]
	private List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	public DirectManipulator directManipulator { get { return m_DirectManipulator; } }
	[SerializeField]
	private DirectManipulator m_DirectManipulator;

	public enum ButtonMode
	{
		TriggerOnHover,
		TriggerOnPress,
	}
	private ButtonMode m_ButtonMode;

	public void Setup(Action<char> keyPress)
	{
		//Set up DirectManipulaotr
		directManipulator.target = transform;
		directManipulator.translate = Translate;
		directManipulator.rotate = Rotate;

		var pressOnHover = m_ButtonMode == ButtonMode.TriggerOnHover;

		foreach (var button in m_Buttons)
		{
			button.Setup(keyPress, pressOnHover);
//			if (pressOnHover)
//				button.trigger = button.onEnter;
		}
	}

	public void SetKeyTextToUpperCase()
	{
		foreach (var button in m_Buttons)
		{
			if (button.textComponent != null)
			{
				if (button.textComponent.text.Length != 1) continue;
				var c = button.textComponent.text[0];
//				if (c >= 'a' && c <= 'z')
				{
					button.textComponent.text = button.textComponent.text.ToUpper();
					button.textComponent.enabled = false;
					button.textComponent.enabled = true;
				}
			}
		}
	}

	public void SetKeyTextToLowerCase()
	{
		foreach (var button in m_Buttons)
		{
			if (button.textComponent != null)
			{
				if (button.textComponent.text.Length != 1) continue;
				var c = button.textComponent.text[0];
//				if (c >= 'A' && c <= 'Z')
				{
					button.textComponent.text = button.textComponent.text.ToLower();
					button.textComponent.enabled = false;
					button.textComponent.enabled = true;
				}
			}
		}
	}

	private void OnEnable()
	{
		if (IsVertical())
			m_ButtonMode = ButtonMode.TriggerOnPress;
		else
			m_ButtonMode = ButtonMode.TriggerOnHover;
	}

	private bool IsVertical()
	{
		return Vector3.Dot(transform.up, Vector3.up) > 0.5f;
	}

	private void Translate(Vector3 deltaPosition)
	{
		transform.position += deltaPosition;
	}

	private void Rotate(Quaternion deltaRotation)
	{
		transform.rotation *= deltaRotation;

//		if (m_ButtonMode == ButtonMode.TriggerOnPress && !IsVertical())
//		{
//			m_ButtonMode = ButtonMode.TriggerOnHover;
//			foreach (var button in m_Buttons)
//				button.trigger = button.onEnter;
//		}
//		else if (m_ButtonMode == ButtonMode.TriggerOnHover && IsVertical())
//		{
//			m_ButtonMode = ButtonMode.TriggerOnPress;
//			foreach (var button in m_Buttons)
//				button.trigger = button.onClick;
//		}
	}
}
