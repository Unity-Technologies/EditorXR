using UnityEngine.VR.UI;

namespace UnityEditor.VR.UI
{
	[CustomEditor(typeof(NumericInputField))]
	[CanEditMultipleObjects]
	public class NumericInputFieldEditor : InputFieldEditor
	{
		SerializedProperty m_NumberTypeProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_NumberTypeProperty = serializedObject.FindProperty("m_NumberType");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_NumberTypeProperty);
			serializedObject.ApplyModifiedProperties();
			base.OnInspectorGUI();
		}
	}
}