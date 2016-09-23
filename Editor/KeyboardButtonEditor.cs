using System;
using System.Runtime.InteropServices.ComTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CanEditMultipleObjects]
[CustomEditor(typeof(KeyboardButton))]
public class KeyboardButtonEditor : RayButtonEditor
{
	SerializedProperty m_CharacterDescriptionTypeProperty;
	SerializedProperty m_SpecialKeyTypeProperty;
	SerializedProperty m_CharacterProperty;
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_MatchButtonTextToCharacterProperty;
	SerializedProperty m_ButtonMeshProperty;
	SerializedProperty m_RepeatOnHoldProperty;
	SerializedProperty m_RepeatTimeProperty;

	private string m_KeyCode = "";

	protected override void OnEnable()
	{
		base.OnEnable();
		
		m_CharacterDescriptionTypeProperty = serializedObject.FindProperty("m_CharacterDescriptionType");
		m_SpecialKeyTypeProperty = serializedObject.FindProperty("m_SpecialKeyType");
		m_CharacterProperty = serializedObject.FindProperty("m_Character");
		m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
		m_MatchButtonTextToCharacterProperty = serializedObject.FindProperty("m_MatchButtonTextToCharacter");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_ButtonMesh");
		m_RepeatOnHoldProperty = serializedObject.FindProperty("m_RepeatOnHold");
		m_RepeatTimeProperty = serializedObject.FindProperty("m_RepeatTime");
	}

	public override void OnInspectorGUI()
	{
//		m_KeyCode = ((char)m_CharacterProperty.intValue).ToString();
		m_KeyCode = EditorGUILayout.TextField("Key Code", m_KeyCode);

		serializedObject.Update();

		if (m_KeyCode.StartsWith("\\") && m_KeyCode.Length > 1)
		{
			m_CharacterDescriptionTypeProperty.enumValueIndex = (int)KeyboardButton.CharacterDescriptionType.Special;

			if (m_KeyCode[1] == 'u')
			{
				if (m_KeyCode.Length > 2)
				{
					int i;
					if (int.TryParse(m_KeyCode.Substring(2), out i))
					{
						if (Enum.IsDefined(typeof (KeyboardButton.SpecialKeyType), i))
						{
							var k = GetIndexOfEnumValue(i);
							if (k != -1)
								m_SpecialKeyTypeProperty.enumValueIndex = GetIndexOfEnumValue(i);
						}
					}
				}
			}
			else
			{
				var valid = true;
				switch (m_KeyCode[1])
				{
					case 'b':
						m_SpecialKeyTypeProperty.enumValueIndex = 1;
						break;
					case 't':
						m_SpecialKeyTypeProperty.enumValueIndex = 2;
						break;
					case 'n':
						m_SpecialKeyTypeProperty.enumValueIndex = 3;
						break;
					case 'r':
						m_SpecialKeyTypeProperty.enumValueIndex = 4;
						break;
					case 's':
						m_SpecialKeyTypeProperty.enumValueIndex = 9;
						break;
					default:
						valid = false;
						break;
				}

				if (m_KeyCode.Length > 2)
					m_KeyCode = m_KeyCode.Remove(2);

				if (!valid)
					m_KeyCode = m_KeyCode.Remove(1);
			}

			m_CharacterProperty.intValue =
				(int)Enum.GetValues(typeof (KeyboardButton.SpecialKeyType)).GetValue(m_SpecialKeyTypeProperty.enumValueIndex);
		}
		else
		{
			m_CharacterDescriptionTypeProperty.enumValueIndex = (int)KeyboardButton.CharacterDescriptionType.Character;

			if (m_KeyCode.Length > 0)
				m_CharacterProperty.intValue = m_KeyCode[0];
			if (m_KeyCode.Length > 1)
				m_KeyCode = m_KeyCode.Remove(1, m_KeyCode.Length - 1);
			

		}

		EditorGUILayout.LabelField(m_CharacterProperty.intValue.ToString());
		EditorGUILayout.LabelField(m_KeyCode);
		Repaint();

		EditorGUILayout.PropertyField(m_CharacterProperty);
		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);
		EditorGUILayout.PropertyField(m_RepeatTimeProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}

	private int GetIndexOfEnumValue(int i)
	{
		int k = 0;
		var enumValues = Enum.GetValues(typeof(KeyboardButton.SpecialKeyType));
		foreach (var val in enumValues)
		{
			if (i == (int)val)
				return k;
			k++;
		}

		return -1;
	}
}
