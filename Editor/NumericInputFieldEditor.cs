using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(NumericInputField))]
public class NumericInputFieldEditor : RayButtonEditor
{
	SerializedProperty m_TextProperty;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_TextProperty = serializedObject.FindProperty("m_TextComponent");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_TextProperty);
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}
