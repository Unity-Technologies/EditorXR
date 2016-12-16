using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

[CustomPropertyDrawer(typeof(FlagsPropertyAttribute))]
public class FlagsPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		property.intValue = U.UI.MaskField(position, label, property.intValue, property.enumNames, U.UI.SerializedPropertyToType(property));
	}
}