using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(RayButton))]
public class RayButtonEditor : ButtonEditor
{
	public override void OnInspectorGUI()
	{
		// Because Button already has a custom editor, and we need to expose SelectionFlags, we need a custom inspector
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelectionFlags"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}