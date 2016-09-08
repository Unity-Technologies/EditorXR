using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(RayButton))]
public class RayButtonEditor : ButtonEditor {
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_HandleFlags"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}