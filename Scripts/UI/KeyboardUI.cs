using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class KeyboardUI : MonoBehaviour
{
	[SerializeField]
	List<KeyboardButton> m_Buttons = new List<KeyboardButton>();

	[SerializeField]
	DirectManipulator m_DirectManipulator;

	[SerializeField]
	Text m_PreviewText;

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

	public void SetPreviewText(string str)
	{
		m_PreviewText.text = str;
	}

	private bool IsHorizontal()
	{
		return Vector3.Dot(transform.up, Vector3.up) < 0.7f;
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
