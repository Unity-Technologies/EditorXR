using UnityEditor;

[CustomEditor(typeof(NumericInputButton))]
public class NumericInputButtonEditor : RayButtonEditor
{
	SerializedProperty m_ButtonTextProperty;
	SerializedProperty m_ButtonMeshProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_ButtonTextProperty = serializedObject.FindProperty("m_ButtonText");
		m_ButtonMeshProperty = serializedObject.FindProperty("m_ButtonMesh");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_ButtonTextProperty);
		EditorGUILayout.PropertyField(m_ButtonMeshProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
