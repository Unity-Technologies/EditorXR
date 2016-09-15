using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VR.Utilities;

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

/// <summary>
/// Here for texting
/// </summary>
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(NumericKeyboardUI))]
public class NumericKeyboardUIEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		NumericKeyboardUI myScript = (NumericKeyboardUI)target;

		GUILayout.BeginHorizontal();

		if (myScript.isActiveAndEnabled)
		{
			if (GUILayout.Button(""))
			{
//				myScript.Setup(new char[] {'1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '1'}, null);
			}
		}
		GUILayout.EndHorizontal();
	}
}
#endif
