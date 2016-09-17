using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(NumericInputField))]
public class NumericInputFieldEditor : SelectableEditor
{
	SerializedProperty m_SelectionFlagsProperty;
	SerializedProperty m_NumericTypeProperty;
	SerializedProperty m_TextProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
		m_NumericTypeProperty = serializedObject.FindProperty("m_InputType");
		m_TextProperty = serializedObject.FindProperty("m_TextComponent");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_SelectionFlagsProperty);
		EditorGUILayout.PropertyField(m_NumericTypeProperty);
		EditorGUILayout.PropertyField(m_TextProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
