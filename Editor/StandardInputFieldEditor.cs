using UnityEngine.VR.UI;

namespace UnityEditor.VR.UI
{
	[CustomEditor(typeof(StandardInputField))]
	public class StandardInputFieldEditor : InputFieldEditor
	{
		SerializedProperty m_LineTypeProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_LineTypeProperty = serializedObject.FindProperty("m_LineType");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_LineTypeProperty);
			serializedObject.ApplyModifiedProperties();
			base.OnInspectorGUI();
		}
	}
}