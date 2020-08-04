using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.UI
{
    [CustomEditor(typeof(KeyboardButton))]
    sealed class KeyboardButtonEditor : Editor
    {
        const char k_LowercaseStart = 'a';
        const char k_LowercaseEnd = 'z';

        SerializedProperty m_SelectionFlagsProperty;
        SerializedProperty m_CharacterProperty;
        SerializedProperty m_UseShiftCharacterProperty;
        SerializedProperty m_ShiftCharacterProperty;
        SerializedProperty m_ButtonTextProperty;
        SerializedProperty m_ButtonMeshProperty;
        SerializedProperty m_RepeatOnHoldProperty;
        SerializedProperty m_WorkspaceButtonProperty;

        KeyboardButton m_KeyboardButton;
        bool m_ShiftCharIsUppercase;

        void OnEnable()
        {
            m_SelectionFlagsProperty = serializedObject.FindProperty("m_SelectionFlags");
            m_CharacterProperty = serializedObject.FindProperty("m_Character");
            m_UseShiftCharacterProperty = serializedObject.FindProperty("m_UseShiftCharacter");
            m_ShiftCharacterProperty = serializedObject.FindProperty("m_ShiftCharacter");
            m_ButtonTextProperty = serializedObject.FindProperty("m_TextComponent");
            m_ButtonMeshProperty = serializedObject.FindProperty("m_TargetMesh");
            m_RepeatOnHoldProperty = serializedObject.FindProperty("m_RepeatOnHold");
            m_WorkspaceButtonProperty = serializedObject.FindProperty("m_WorkspaceButton");
        }

        public override void OnInspectorGUI()
        {
            m_KeyboardButton = (KeyboardButton)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SelectionFlagsProperty);

            var updateObjectName = false;
            EditorGUI.BeginChangeCheck();
            CharacterField("Primary Character", m_CharacterProperty, true);
            if (EditorGUI.EndChangeCheck())
                updateObjectName = true;

            EditorGUILayout.PropertyField(m_ButtonTextProperty);

            // Set text component to character
            if (m_KeyboardButton.textComponent != null)
            {
                EditorGUI.BeginChangeCheck();
                if (EditorGUI.EndChangeCheck())
                    UpdateButtonTextAndObjectName(m_CharacterProperty.intValue, updateObjectName);
            }

            // Handle shift character
            m_UseShiftCharacterProperty.boolValue = EditorGUILayout.Toggle("Use Shift Character", m_UseShiftCharacterProperty.boolValue);
            if (m_UseShiftCharacterProperty.boolValue)
            {
                var ch = (char)m_CharacterProperty.intValue;
                if (ch >= k_LowercaseStart && ch <= k_LowercaseEnd)
                {
                    var upperCase = ((char)m_CharacterProperty.intValue).ToString().ToUpper();
                    m_ShiftCharIsUppercase = upperCase.Equals(((char)m_ShiftCharacterProperty.intValue).ToString());
                    EditorGUI.BeginChangeCheck();
                    m_ShiftCharIsUppercase = EditorGUILayout.Toggle("Shift Character is Uppercase", m_ShiftCharIsUppercase);
                    if (EditorGUI.EndChangeCheck())
                        m_ShiftCharacterProperty.intValue = m_ShiftCharIsUppercase ? upperCase[0] : 0;
                }
                else
                {
                    m_ShiftCharIsUppercase = false;
                }

                if (!m_ShiftCharIsUppercase)
                    CharacterField("Shift Character", m_ShiftCharacterProperty, false);
            }
            else
            {
                m_ShiftCharIsUppercase = false;
            }

            EditorGUILayout.PropertyField(m_ButtonMeshProperty);
            EditorGUILayout.PropertyField(m_RepeatOnHoldProperty);
            EditorGUILayout.PropertyField(m_WorkspaceButtonProperty);

            if (GUILayout.Button("Create layout transfrom"))
            {
                // Get position in hierarchy
                var siblingIndex = 0;
                foreach (Transform child in m_KeyboardButton.transform.parent)
                {
                    if (child == m_KeyboardButton.transform)
                        break;
                    siblingIndex++;
                }

                var t = new GameObject(m_KeyboardButton.name + "_LayoutPosition").transform;
                t.SetParent(m_KeyboardButton.transform);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                t.SetParent(m_KeyboardButton.transform.parent);
                t.transform.SetSiblingIndex(siblingIndex);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CharacterField(string label, SerializedProperty property, bool updateName)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var inputString = ((char)property.intValue).ToString();
            inputString = EditorGUILayout.TextField(label, inputString);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = (int)GetKeycodeFromString(inputString);
                UpdateButtonTextAndObjectName(property.intValue, updateName);
            }

            EditorGUI.BeginChangeCheck();
            property.intValue = (int)(KeyCode)EditorGUILayout.EnumPopup((KeyCode)property.intValue);
            if (EditorGUI.EndChangeCheck())
                UpdateButtonTextAndObjectName(property.intValue, updateName);
            EditorGUILayout.EndHorizontal();
        }

        KeyCode GetKeycodeFromString(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                return KeyCode.None;

            try
            {
                inputString = Regex.Unescape(inputString);
                return (KeyCode)inputString[0];
            }
            catch (ArgumentException)
            {
                // Incomplete (i.e. user is still typing it out likely) or badly formed unicode string
            }

            return KeyCode.None;
        }

        void UpdateButtonTextAndObjectName(int input, bool updateName)
        {
            var inputString = ((char)input).ToString();

            // For valid keycodes, use the string version of those for
            if (Enum.IsDefined(typeof(KeyCode), input))
                inputString = ((KeyCode)input).ToString();

            if (updateName)
                m_KeyboardButton.gameObject.name = inputString;
        }
    }
}
