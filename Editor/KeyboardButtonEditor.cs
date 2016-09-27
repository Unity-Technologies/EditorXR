using System;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(KeyboardButton))]
public class KeyboardButtonEditor : RayButtonEditor
{
	SerializedProperty m_CharacterProperty;
	SerializedProperty m_UseShiftCharacterProperty;
	SerializedProperty m_ShiftCharacterProperty;
	SerializedProperty m_ShiftCharIsUppercaseProperty;
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_MatchButtonTextToCharacterProperty;
	SerializedProperty m_ButtonIconProperty;
	SerializedProperty m_ButtonMeshProperty;
	SerializedProperty m_RepeatOnHoldProperty;
	SerializedProperty m_RepeatTimeProperty;

	private KeyCode m_KeyCode;
	private KeyCode m_ShiftKeyCode;

	private string m_KeyCodeStr;
	private string m_ShiftKeyCodeStr;

	private KeyboardButton m_KeyboardButton;

	protected override void OnEnable()
	{
		base.OnEnable();
		
		m_CharacterProperty = serializedObject.FindProperty("m_Character");
		m_UseShiftCharacterProperty = serializedObject.FindProperty("m_UseShiftCharacter");
		m_ShiftCharacterProperty = serializedObject.FindProperty("m_ShiftCharacter");
		m_ShiftCharIsUppercaseProperty = serializedObject.FindProperty("m_ShiftCharIsUppercase");
		m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
		m_MatchButtonTextToCharacterProperty = serializedObject.FindProperty("m_MatchButtonTextToCharacter");
		m_ButtonIconProperty = serializedObject.FindProperty("m_ButtonIcon");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_ButtonMesh");
		m_RepeatOnHoldProperty = serializedObject.FindProperty("m_RepeatOnHold");
		m_RepeatTimeProperty = serializedObject.FindProperty("m_RepeatTime");

		m_KeyCode = (KeyCode)m_CharacterProperty.intValue;
		m_KeyCodeStr = ((char)m_KeyCode).ToString();

		m_ShiftKeyCode = (KeyCode)m_ShiftCharacterProperty.intValue;
		m_ShiftKeyCodeStr = ((char)m_ShiftKeyCode).ToString();
	}

	public override void OnInspectorGUI()
	{
		m_KeyboardButton = (KeyboardButton)target;

		serializedObject.Update();

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginChangeCheck();
		m_KeyCodeStr = EditorGUILayout.TextField("Key Code", m_KeyCodeStr);
		if (EditorGUI.EndChangeCheck())
		{
			m_CharacterProperty.intValue = GetCharacterValueFromText(ref m_KeyCode, ref m_KeyCodeStr);
			UpdateButtonTextAndObjectName(m_KeyCode);
		}

		EditorGUI.BeginChangeCheck();
		m_KeyCode = (KeyCode)EditorGUILayout.EnumPopup(m_KeyCode);
		if (EditorGUI.EndChangeCheck())
		{
			m_KeyCodeStr = ((char)m_KeyCode).ToString();
			m_CharacterProperty.intValue = (int)m_KeyCode;
			UpdateButtonTextAndObjectName(m_KeyCode);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		// Set text component to character
		if (m_KeyboardButton.textComponent != null)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
			if (EditorGUI.EndChangeCheck())
				UpdateButtonTextAndObjectName(m_KeyCode);

			if (m_MatchButtonTextToCharacterProperty.boolValue)
			{
				if (!m_KeyboardButton.textComponent.font.HasCharacter((char)m_CharacterProperty.intValue))
					EditorGUILayout.HelpBox("Character not defined in font, consider using an icon", MessageType.Error);
			}
		}

		// Handle shift character
		m_UseShiftCharacterProperty.boolValue = EditorGUILayout.Toggle("Use Shift Character", m_UseShiftCharacterProperty.boolValue);
		if (m_UseShiftCharacterProperty.boolValue)
		{
			var ch = (char)m_CharacterProperty.intValue;
			if (ch > 'a' && ch < 'z' || ch > 'A' && ch < 'Z')
				m_ShiftCharIsUppercaseProperty.boolValue = EditorGUILayout.Toggle("Shift Character is Uppercase", m_ShiftCharIsUppercaseProperty.boolValue);
			else
				m_ShiftCharIsUppercaseProperty.boolValue = false;

			if (!m_ShiftCharIsUppercaseProperty.boolValue)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginChangeCheck();
				m_ShiftKeyCodeStr = EditorGUILayout.TextField("Shift Key Code", m_ShiftKeyCodeStr);
				if (EditorGUI.EndChangeCheck())
					m_ShiftCharacterProperty.intValue = GetCharacterValueFromText(ref m_ShiftKeyCode, ref m_ShiftKeyCodeStr);

				EditorGUI.BeginChangeCheck();
				m_ShiftKeyCode = (KeyCode)EditorGUILayout.EnumPopup(m_ShiftKeyCode);
				if (EditorGUI.EndChangeCheck())
				{
					m_ShiftKeyCodeStr = ((char)m_ShiftKeyCode).ToString();
					m_ShiftCharacterProperty.intValue = (int)m_ShiftKeyCode;
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		else
		{
			m_ShiftCharIsUppercaseProperty.boolValue = false;
		}

		EditorGUILayout.PropertyField(m_ButtonIconProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);

		if (m_RepeatOnHoldProperty.boolValue)
			EditorGUILayout.PropertyField(m_RepeatTimeProperty);

		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}

	private int GetCharacterValueFromText(ref KeyCode keyCode, ref string keyCodeStr)
	{
		if (keyCodeStr.StartsWith("\\") && keyCodeStr.Length > 1)
		{
			if (keyCodeStr[1] == 'u')
			{
				if (keyCodeStr.Length > 2)
				{
					int i;
					if (int.TryParse(keyCodeStr.Substring(2), out i))
					{
						if (Enum.IsDefined(typeof(KeyCode), i))
						{
							keyCode = (KeyCode)i;
							return (int)keyCode;
						}
					}
				}
			}
			else
			{
				var valid = true;
				switch (keyCodeStr[1])
				{
					case 'b':
						keyCode = KeyCode.Backspace;
						break;
					case 't':
						keyCode = KeyCode.Tab;
						break;
					case 'n': // KeyCode doesn't define newline
					case 'r':
						keyCode = KeyCode.Return;
						break;
					case 's':
						keyCode = KeyCode.Space;
						break;
					default:
						valid = false;
						break;
				}

				if (keyCodeStr.Length > 2)
					keyCodeStr = keyCodeStr.Remove(2);

				if (valid)
					return (int)keyCode;

				EditorGUILayout.HelpBox("Invalid entry", MessageType.Error);
			}
		}
		else
		{
			if (keyCodeStr.Length > 0)
			{
				if (keyCodeStr.Length > 1)
					keyCodeStr = keyCodeStr.Remove(1);

				keyCode = (KeyCode)keyCodeStr[0];
				return (int)keyCode;
			}
		}

		return -1;
	}

	private void UpdateButtonTextAndObjectName(KeyCode keyCode)
	{
		if (m_MatchButtonTextToCharacterProperty.boolValue)
			m_KeyboardButton.textComponent.text = ((char)(int)keyCode).ToString();

		m_KeyboardButton.gameObject.name = keyCode.ToString();
	}
}
