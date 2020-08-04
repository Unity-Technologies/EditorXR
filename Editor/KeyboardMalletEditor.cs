using UnityEditor;

namespace Unity.EditorXR.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(KeyboardMallet))]
    sealed class KeyboardMalletEditor : Editor
    {
        SerializedProperty m_StemOriginProperty;
        SerializedProperty m_StemLengthProperty;
        SerializedProperty m_BulbProperty;
        SerializedProperty m_BulbRadiusProperty;
        SerializedProperty m_BulbColliderProperty;
        SerializedProperty m_StemWidthProperty;

        private KeyboardMallet m_KeyboardMallet;

        public void OnEnable()
        {
            m_StemOriginProperty = serializedObject.FindProperty("m_StemOrigin");
            m_StemLengthProperty = serializedObject.FindProperty("m_StemLength");
            m_BulbProperty = serializedObject.FindProperty("m_Bulb");
            m_BulbRadiusProperty = serializedObject.FindProperty("m_BulbRadius");
            m_BulbColliderProperty = serializedObject.FindProperty("m_BulbCollider");
            m_StemWidthProperty = serializedObject.FindProperty("m_StemWidth");
        }

        public override void OnInspectorGUI()
        {
            m_KeyboardMallet = (KeyboardMallet)target;

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_StemOriginProperty);
            EditorGUILayout.PropertyField(m_StemLengthProperty);
            EditorGUILayout.PropertyField(m_StemWidthProperty);
            EditorGUILayout.PropertyField(m_BulbProperty);
            EditorGUILayout.PropertyField(m_BulbRadiusProperty);
            m_KeyboardMallet.UpdateMalletDimensions();

            EditorGUILayout.PropertyField(m_BulbColliderProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
