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

	protected void OnEnable()
	{
		m_ButtonsProperty = serializedObject.FindProperty("m_Buttons");
		m_VerticalLayoutTransformsProperty = serializedObject.FindProperty("m_VerticalLayoutTransforms");
		m_HorizontalLayoutTransformsProperty = serializedObject.FindProperty("m_HorizontalLayoutTransforms");
		m_DirectManipulatorProperty = serializedObject.FindProperty("m_DirectManipulator");
	}

	public override void OnInspectorGUI()
	{
		var labelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 100f;
		serializedObject.Update();
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PropertyField(m_ButtonsProperty, true);
		EditorGUILayout.PropertyField(m_VerticalLayoutTransformsProperty, true);
		EditorGUILayout.PropertyField(m_HorizontalLayoutTransformsProperty, true);
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.PropertyField(m_DirectManipulatorProperty);
		serializedObject.ApplyModifiedProperties();
		EditorGUIUtility.labelWidth = labelWidth;
	}

	private static void ShowElements(SerializedProperty list)
	{
		for (int i = 0; i < list.arraySize; i++)
		{
			EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
			EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
		}
	}
}