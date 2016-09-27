using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
public class KeyboardUI : MonoBehaviour
{
	[SerializeField]
	private List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	[SerializeField]
	private DirectManipulator m_DirectManipulator;

	public void Setup(Action<char> keyPress)
	{
		m_DirectManipulator.target = transform;
		m_DirectManipulator.translate = Translate;
		m_DirectManipulator.rotate = Rotate;

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
		return Vector3.Dot(transform.up, Vector3.up) < 0.5f;
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
