using UnityEditor;
using UnityEditor.Graphs;

[CustomEditor(typeof(NumericInputButton))]
public class NumericInputButtonEditor : RayButtonEditor
{
	SerializedProperty m_CharacterDescriptionTypeProperty;
	SerializedProperty m_SpecialKeyTypeProperty;
	SerializedProperty m_KeyCodeProperty;
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_MatchButtonTextToCharacterProperty;
	SerializedProperty m_ButtonMeshProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_CharacterDescriptionTypeProperty = serializedObject.FindProperty("m_CharacterDescriptionType");
		m_KeyCodeProperty = serializedObject.FindProperty("m_KeyCode");
		m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
		m_MatchButtonTextToCharacterProperty = serializedObject.FindProperty("m_MatchButtonTextToCharacter");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_ButtonMesh");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_CharacterDescriptionTypeProperty);
		EditorGUILayout.PropertyField(m_KeyCodeProperty);
		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		EditorGUILayout.PropertyField(m_MatchButtonTextToCharacterProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
