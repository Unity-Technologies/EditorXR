using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(RaySlider))]
public class RaySliderEditor : SliderEditor {
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelectionFlags"));
		serializedObject.ApplyModifiedProperties();
		base.OnInspectorGUI();
	}
}