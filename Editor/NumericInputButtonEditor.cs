using UnityEditor;

[CustomEditor(typeof(NumericInputButton))]
public class NumericInputButtonEditor : RayButtonEditor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ButtonText"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ButtonMesh"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
