using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FlagsPropertyAttribute))]
public class FlagsPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
	}
}