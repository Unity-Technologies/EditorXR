using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
    [CustomEditor(typeof(EditingContextManagerSettings))]
    sealed class EditingContextManagerEditor : Editor
    {
        string[] m_ContextNames;
        int m_SelectedContextIndex;

        EditingContextManagerSettings m_Settings;

        void Awake()
        {
            m_ContextNames = EditingContextManager.GetEditingContextNames();
            m_Settings = (EditingContextManagerSettings)target;
            m_SelectedContextIndex = Array.IndexOf(m_ContextNames, m_Settings.defaultContextName);
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Available Contexts");

            m_SelectedContextIndex = EditorGUILayout.Popup(string.Empty, m_SelectedContextIndex, m_ContextNames);
            if (GUI.changed)
            {
                m_Settings.defaultContextName = m_ContextNames[m_SelectedContextIndex];
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save"))
                {
                    EditingContextManager.SaveProjectSettings(m_Settings);
                    Selection.activeObject = null;
                }

                if (GUILayout.Button("Reset"))
                {
                    EditingContextManager.ResetProjectSettings();
                    Selection.activeGameObject = null;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
