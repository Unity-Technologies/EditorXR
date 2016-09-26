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

	private Action<char> keyPress;

	public enum KeyboardButtonMode
	{
		TriggerOnHover,
		TriggerOnPress,
	}
	private KeyboardButtonMode m_KeyboardButtonMode;

	public void Setup(Action<char> keyPress)
	{
		this.keyPress = keyPress;

		directManipulator.target = transform;
		directManipulator.translate = Translate;
		directManipulator.rotate = Rotate;

		UpdateKeyboardButtonMode();
	}

	public void ActivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(true);
	}

	public void DeactivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(false);
	}

	private void OnEnable()
	{
		if (IsVertical())
			m_KeyboardButtonMode = KeyboardButtonMode.TriggerOnPress;
		else
			m_KeyboardButtonMode = KeyboardButtonMode.TriggerOnHover;
	}

	private void UpdateKeyboardButtonMode()
	{
		var pressOnHover = m_KeyboardButtonMode == KeyboardButtonMode.TriggerOnHover;
		foreach (var button in m_Buttons)
			button.Setup(keyPress, pressOnHover);
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

		if (m_KeyboardButtonMode == KeyboardButtonMode.TriggerOnPress && !IsVertical())
		{
			m_KeyboardButtonMode = KeyboardButtonMode.TriggerOnHover;
			UpdateKeyboardButtonMode();
		}
		else if (m_KeyboardButtonMode == KeyboardButtonMode.TriggerOnHover && IsVertical())
		{
			m_KeyboardButtonMode = KeyboardButtonMode.TriggerOnPress;
			UpdateKeyboardButtonMode();
		}
	}
}
