using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(NumericInputField))]
public class NumericInputFieldEditor : InputFieldEditor
{
	SerializedProperty m_SelectionFlagsProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_SelectionFlagsProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
