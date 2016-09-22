using System;
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

	private KeyboardButton keyboardButton;
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
		keyboardButton = (KeyboardButton)target;

		m_KeyCode = EditorGUILayout.TextField("Key Code", m_KeyCode);

		serializedObject.Update();

		if (m_KeyCode.StartsWith("\\") && m_KeyCode.Length > 1)
		{
			m_CharacterDescriptionTypeProperty.enumValueIndex = (int)KeyboardButton.CharacterDescriptionType.Special;

			if (m_KeyCode[1] == 'u')
			{
				if (m_KeyCode.Length > 2)
				{
					var i = 0;
					if (int.TryParse(m_KeyCode.Substring(2), out i))
					{
//						Debug.Log("Index value " + i);
//						m_SpecialKeyTypeProperty.enumValueIndex = 
//						serializedObject.Update();

						Debug.Log(m_SpecialKeyTypeProperty.enumValueIndex);

						if (Enum.IsDefined(typeof (KeyboardButton.SpecialKeyType), i))
						{

//							m_SpecialKeyTypeProperty. = Enum.GetValues(typeof (KeyboardButton.SpecialKeyType)).GetValue(i);
							keyboardButton.specialKeyType = (KeyboardButton.SpecialKeyType)i;
							m_CharacterProperty.intValue = (int)keyboardButton.specialKeyType;
						}
					}
				}
			}
			else
			{
				var valid = false;

				switch (m_KeyCode[1])
				{
					case 'b':
						keyboardButton.specialKeyType = KeyboardButton.SpecialKeyType.Backspace;
						break;
					case 'r':
						keyboardButton.specialKeyType = KeyboardButton.SpecialKeyType.CarriageReturn;
						break;
					case 'n':
						keyboardButton.specialKeyType = KeyboardButton.SpecialKeyType.NewLine;
						break;
					case 's':
						keyboardButton.specialKeyType = KeyboardButton.SpecialKeyType.Space;
						break;
					case 't':
						keyboardButton.specialKeyType = KeyboardButton.SpecialKeyType.Tab;
						break;
				}
			}
		}
		else
		{
			m_CharacterDescriptionTypeProperty.enumValueIndex = (int)KeyboardButton.CharacterDescriptionType.Character;

			if (m_KeyCode.Length > 1)
			{
				m_KeyCode = m_KeyCode.Remove(1, m_KeyCode.Length - 1);
				m_CharacterProperty.intValue = m_KeyCode[0];
			}
		}

		Repaint();

		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);
		EditorGUILayout.PropertyField(m_RepeatTimeProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
