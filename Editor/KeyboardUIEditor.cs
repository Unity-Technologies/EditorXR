using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR.UI
{
    [CustomEditor(typeof(KeyboardUI))]
    sealed class KeyboardUIEditor : Editor
    {
        SerializedProperty m_ButtonsProperty;
        SerializedProperty m_VerticalLayoutTransformsProperty;
        SerializedProperty m_HorizontalLayoutTransformsProperty;
        SerializedProperty m_DirectManipulatorProperty;
        SerializedProperty m_SmoothMotionProperty;

        KeyboardUI m_KeyboardUI;

        void OnEnable()
        {
            m_ButtonsProperty = serializedObject.FindProperty("m_Buttons");
            m_VerticalLayoutTransformsProperty = serializedObject.FindProperty("m_VerticalLayoutTransforms");
            m_HorizontalLayoutTransformsProperty = serializedObject.FindProperty("m_HorizontalLayoutTransforms");
            m_DirectManipulatorProperty = serializedObject.FindProperty("m_DirectManipulator");
            m_SmoothMotionProperty = serializedObject.FindProperty("m_SmoothMotion");
        }

        public override void OnInspectorGUI()
        {
            m_KeyboardUI = (KeyboardUI)target;

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            serializedObject.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Button");
            EditorGUILayout.LabelField("Vertical Slots");
            EditorGUILayout.LabelField("Horizontal Slots");
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < m_ButtonsProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_ButtonsProperty.GetArrayElementAtIndex(i));
                EditorGUILayout.PropertyField(m_VerticalLayoutTransformsProperty.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUILayout.PropertyField(m_HorizontalLayoutTransformsProperty.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.PropertyField(m_DirectManipulatorProperty);
            EditorGUILayout.PropertyField(m_SmoothMotionProperty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Vertical layout"))
                m_KeyboardUI.ForceMoveButtonsToVerticalLayout();
            if (GUILayout.Button("Horizontal layout"))
                m_KeyboardUI.ForceMoveButtonsToHorizontalLayout();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
