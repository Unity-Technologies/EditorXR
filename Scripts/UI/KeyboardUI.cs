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

	/// <summary>
	/// Initialize the keyboard and its buttons
	/// </summary>
	/// <param name="keyPress"></param>
	public void Setup(Action<char> keyPress)
	{
		m_DirectManipulator.target = transform;
		m_DirectManipulator.translate = Translate;
		m_DirectManipulator.rotate = Rotate;

		foreach (var button in m_Buttons) 
			button.Setup(keyPress, IsHorizontal);
	}

	/// <summary>
	/// Activate shift mode on all buttons
	/// </summary>
	public void ActivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(true);
	}

	/// <summary>
	/// Deactivate shift mode on all buttons
	/// </summary>
	public void DeactivateShiftModeOnKeys()
	{
		foreach (var button in m_Buttons)
			button.SetShiftModeActive(false);
	}

	private bool IsHorizontal()
	{
		var isHorizontal = Vector3.Dot(transform.up, Vector3.up) < 0.7f;
		return isHorizontal;
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
