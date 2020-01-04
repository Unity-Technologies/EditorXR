using UnityEditor;

namespace Unity.Labs.EditorXR.UI
{
    // Because Button already has a custom editor, and we need to expose SelectionFlags, we need a custom inspector
    [CustomEditor(typeof(Button))]
    sealed class ButtonEditor : UnityEditor.UI.ButtonEditor
    {
        SerializedProperty m_SelectionFlagsProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_SelectionFlagsProperty);
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
