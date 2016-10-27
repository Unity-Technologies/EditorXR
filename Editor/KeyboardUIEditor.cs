using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

[CustomEditor(typeof(KeyboardUI))]
public class KeyboardUIEditor : Editor
{
	SerializedProperty m_ButtonsProperty;
	SerializedProperty m_VerticalLayoutTransformsProperty;
	SerializedProperty m_HorizontalLayoutTransformsProperty;
	SerializedProperty m_DirectManipulatorProperty;

	KeyboardUI m_KeyboardUI;

	protected void OnEnable()
	{
		m_ButtonsProperty = serializedObject.FindProperty("m_Buttons");
		m_VerticalLayoutTransformsProperty = serializedObject.FindProperty("m_VerticalLayoutTransforms");
		m_HorizontalLayoutTransformsProperty = serializedObject.FindProperty("m_HorizontalLayoutTransforms");
		m_DirectManipulatorProperty = serializedObject.FindProperty("m_DirectManipulator");
	}

	public override void OnInspectorGUI()
	{
		m_KeyboardUI = (KeyboardUI)target;

		var labelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 100f;
		serializedObject.Update();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Button");
		EditorGUILayout.LabelField("Vertical Slots");
		EditorGUILayout.LabelField("Horizontal Slots");
		EditorGUILayout.EndHorizontal();
		for (int i = 0; i < m_ButtonsProperty.arraySize - 1; i++)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_ButtonsProperty.GetArrayElementAtIndex(i));
			EditorGUILayout.PropertyField(m_VerticalLayoutTransformsProperty.GetArrayElementAtIndex(i), GUIContent.none);
			EditorGUILayout.PropertyField(m_HorizontalLayoutTransformsProperty.GetArrayElementAtIndex(i), GUIContent.none);
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.PropertyField(m_DirectManipulatorProperty);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Vertical layout"))
			m_KeyboardUI.ForceMoveButtonsToVerticalLayout();
		if (GUILayout.Button("Horizontal layout"))
			m_KeyboardUI.ForceMoveButtonsToHorizontalLayout();
		EditorGUILayout.EndHorizontal();

		serializedObject.ApplyModifiedProperties();
		EditorGUIUtility.labelWidth = labelWidth;
	}
}