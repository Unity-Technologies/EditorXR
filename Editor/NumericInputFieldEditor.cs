using UnityEditor;

namespace Unity.Labs.EditorXR.UI
{
    [CustomEditor(typeof(NumericInputField))]
    [CanEditMultipleObjects]
    sealed class NumericInputFieldEditor : InputFieldEditor
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
