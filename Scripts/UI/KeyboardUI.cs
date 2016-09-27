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

	public void Setup(Action<char> keyPress)
	{
		directManipulator.target = transform;
		directManipulator.translate = Translate;
		directManipulator.rotate = Rotate;

		foreach (var button in m_Buttons) 
			button.Setup(keyPress, IsHorizontal);
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

	private bool IsHorizontal()
	{
		return Vector3.Dot(transform.up, Vector3.up) <= 0.5f;
	}

	private void Translate(Vector3 deltaPosition)
	{
		transform.position += deltaPosition;
	}

	private void Rotate(Quaternion deltaRotation)
	{
		transform.rotation *= deltaRotation;
	}
}
