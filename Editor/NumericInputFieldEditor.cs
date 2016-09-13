using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(NumericInputField))]
public class NumericInputFieldEditor : InputFieldEditor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelectionFlags"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
