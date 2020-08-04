using UnityEditor;

namespace Unity.EditorXR.UI
{
    [CustomEditor(typeof(StandardInputField))]
    sealed class StandardInputFieldEditor : InputFieldEditor
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
