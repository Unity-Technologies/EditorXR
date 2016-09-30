using UnityEditor;
using UnityEngine.VR.Handles;

[CanEditMultipleObjects]
[CustomEditor(typeof(BaseHandle))]
public class BaseHandleEditor : Editor
{
	SerializedProperty m_SelectionFlagsProperty;

	protected virtual void OnEnable()
	{
		m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_SelectionFlagsProperty);

		serializedObject.ApplyModifiedProperties();
	}
}
