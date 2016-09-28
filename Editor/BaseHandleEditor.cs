using UnityEditor;
using UnityEngine.VR.Handles;

[CanEditMultipleObjects]
[CustomEditor(typeof(BaseHandle))]
public class BaseHandleEditor : Editor
{
	SerializedProperty m_SelectionFlagsProperty;

//	private string[] m_PropertyPathToExcludeForChildClasses;

	protected virtual void OnEnable()
	{
		m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");

//		m_PropertyPathToExcludeForChildClasses = new[]
//		{
//			m_SelectionFlagsProperty.propertyPath
//		};
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(m_SelectionFlagsProperty);

		ChildClassPropertiesGUI();

		serializedObject.ApplyModifiedProperties();
	}

	private void ChildClassPropertiesGUI()
	{
		if (IsDerivedSelectableEditor())
			return;

//		DrawPropertiesExcluding(serializedObject, m_PropertyPathToExcludeForChildClasses);
	}

	private bool IsDerivedSelectableEditor()
	{
		return GetType() != typeof(BaseHandleEditor);
	}
}
