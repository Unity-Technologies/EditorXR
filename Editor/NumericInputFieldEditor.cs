using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(NumericInputField))]
public class NumericInputFieldEditor : SelectableEditor
{
	SerializedProperty m_SelectionFlagsProperty;
	SerializedProperty m_NumberTypeProperty;
	SerializedProperty m_KeyboardAnchorTransformProperty;
	SerializedProperty m_TextComponentProperty;
	SerializedProperty m_CharacterLimitProperty;
	SerializedProperty m_OnValueChangedProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
		m_NumberTypeProperty = serializedObject.FindProperty("m_NumberType");
		m_KeyboardAnchorTransformProperty = serializedObject.FindProperty("m_KeyboardAnchorTransform");
		m_TextComponentProperty = serializedObject.FindProperty("m_TextComponent");
		m_CharacterLimitProperty = serializedObject.FindProperty("m_CharacterLimit");
		m_OnValueChangedProperty = serializedObject.FindProperty("m_OnValueChanged");

	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_SelectionFlagsProperty);
		EditorGUILayout.PropertyField(m_NumberTypeProperty);
		EditorGUILayout.PropertyField(m_KeyboardAnchorTransformProperty);
		EditorGUILayout.PropertyField(m_TextComponentProperty);
		EditorGUILayout.PropertyField(m_CharacterLimitProperty);
		EditorGUILayout.PropertyField(m_OnValueChangedProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
