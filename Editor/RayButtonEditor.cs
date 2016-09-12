using UnityEditor;
using UnityEditor.UI;

// Because Button already has a custom editor, and we need to expose SelectionFlags, we need a custom inspector
[CustomEditor(typeof(RayButton))]
public class RayButtonEditor : ButtonEditor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelectionFlags"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}