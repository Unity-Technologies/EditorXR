using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(DragTest))]
public class DragTestEditor : SelectableEditor
{
	SerializedProperty m_InputFieldTypeProperty;
	SerializedProperty m_KeyboardAnchorTransformProperty;
	SerializedProperty m_TextComponentProperty;
	SerializedProperty m_CharacterLimitProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_InputFieldTypeProperty = serializedObject.FindProperty("m_ContentType");
		m_KeyboardAnchorTransformProperty = serializedObject.FindProperty("m_KeyboardAnchorTransform");
		m_TextComponentProperty = serializedObject.FindProperty("m_TextComponent");
		m_CharacterLimitProperty = serializedObject.FindProperty("m_CharacterLimit");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_InputFieldTypeProperty);
		EditorGUILayout.PropertyField(m_KeyboardAnchorTransformProperty);
		EditorGUILayout.PropertyField(m_TextComponentProperty);
		EditorGUILayout.PropertyField(m_CharacterLimitProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
