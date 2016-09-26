using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CanEditMultipleObjects]
[CustomEditor(typeof(KeyboardButton))]
public class KeyboardButtonEditor : RayButtonEditor
{
	SerializedProperty m_CharacterProperty;
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_MatchButtonTextToCharacterProperty;
	SerializedProperty m_ButtonIconProperty;
	SerializedProperty m_ButtonMeshProperty;
	SerializedProperty m_RepeatOnHoldProperty;
	SerializedProperty m_RepeatTimeProperty;

	private KeyCode m_KeyCode;
	private string m_KeyCodeStr;
	private KeyboardButton keyboardButton;

	protected override void OnEnable()
	{
		base.OnEnable();
		
		m_CharacterProperty = serializedObject.FindProperty("m_Character");
		m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
		m_MatchButtonTextToCharacterProperty = serializedObject.FindProperty("m_MatchButtonTextToCharacter");
		m_ButtonIconProperty = serializedObject.FindProperty("m_ButtonIcon");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_ButtonMesh");
		m_RepeatOnHoldProperty = serializedObject.FindProperty("m_RepeatOnHold");
		m_RepeatTimeProperty = serializedObject.FindProperty("m_RepeatTime");

		m_KeyCode = (KeyCode)m_CharacterProperty.intValue;
		m_KeyCodeStr = ((char)m_KeyCode).ToString();
	}

	public override void OnInspectorGUI()
	{
		keyboardButton = (KeyboardButton)target;

		serializedObject.Update();

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginChangeCheck();
		m_KeyCodeStr = EditorGUILayout.TextField("Key Code", m_KeyCodeStr);
		if (EditorGUI.EndChangeCheck())
		{
			if (m_KeyCodeStr.StartsWith("\\") && m_KeyCodeStr.Length > 1)
			{
				if (m_KeyCodeStr[1] == 'u')
				{
					if (m_KeyCodeStr.Length > 2)
					{
						int i;
						if (int.TryParse(m_KeyCodeStr.Substring(2), out i))
						{
							if (Enum.IsDefined(typeof(KeyCode), i))
							{
								m_KeyCode = (KeyCode)i;
								UpdateCharacterValue();
							}
						}
					}
				}
				else
				{
					var valid = true;
					switch (m_KeyCodeStr[1])
					{
						case 'b':
							m_KeyCode = KeyCode.Backspace;
							break;
						case 't':
							m_KeyCode = KeyCode.Tab;
							break;
						case 'n': // KeyCode doesn't define newline
						case 'r':
							m_KeyCode = KeyCode.Return;
							break;
						case 's':
							m_KeyCode = KeyCode.Space;
							break;
						default:
							valid = false;
							break;
					}

					if (m_KeyCodeStr.Length > 2)
						m_KeyCodeStr = m_KeyCodeStr.Remove(2);

					if (valid)
						UpdateCharacterValue();
					else
						EditorGUILayout.HelpBox("Invalid entry", MessageType.Error);
				}
			}
			else
			{
				if (m_KeyCodeStr.Length > 0)
				{
					if (m_KeyCodeStr.Length > 1)
						m_KeyCodeStr = m_KeyCodeStr.Remove(1);

					m_KeyCode = (KeyCode)m_KeyCodeStr[0];
					UpdateCharacterValue();
				}
			}
		}

		EditorGUI.BeginChangeCheck();
		m_KeyCode = (KeyCode)EditorGUILayout.EnumPopup(m_KeyCode);
		if (EditorGUI.EndChangeCheck())
		{
			m_KeyCodeStr = ((char)m_KeyCode).ToString();
			UpdateCharacterValue();
		}
		EditorGUILayout.EndHorizontal();

		//For debug
//		EditorGUILayout.LabelField(m_CharacterProperty.intValue.ToString() + " " + ((char)m_CharacterProperty.intValue).ToString());
//		EditorGUILayout.PropertyField(m_CharacterProperty);

		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		// Set text component to character
		if (keyboardButton.textComponent != null)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
			if (EditorGUI.EndChangeCheck())
				UpdateCharacterValue();

			if (m_MatchButtonTextToCharacterProperty.boolValue)
			{
				if (!keyboardButton.textComponent.font.HasCharacter((char)m_CharacterProperty.intValue))
					EditorGUILayout.HelpBox("Character not defined in font, consider using an icon", MessageType.Error);
			}
		}

		EditorGUILayout.PropertyField(m_ButtonIconProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);

		if (m_RepeatOnHoldProperty.boolValue)
			EditorGUILayout.PropertyField(m_RepeatTimeProperty);

		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}

	private void UpdateCharacterValue()
	{
		m_CharacterProperty.intValue = (int)m_KeyCode;

		if (m_MatchButtonTextToCharacterProperty.boolValue)
		{
			keyboardButton.textComponent.text = ((char)m_CharacterProperty.intValue).ToString();
		}

		keyboardButton.gameObject.name = m_KeyCode.ToString();
	}
}
