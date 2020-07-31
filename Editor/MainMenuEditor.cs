using Unity.EditorXR.Interfaces;
using Unity.EditorXR.Menus;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.UI
{
    [CustomEditor(typeof(MainMenu))]
    class MainMenuEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var mainMenu = (MainMenu) target;

            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            if (mainMenu.menuTools != null)
            {
                foreach (var tool in mainMenu.menuTools)
                {
                    if (GUILayout.Button(tool.Name))
                        mainMenu.SelectTool(mainMenu.rayOrigin, tool);
                }
            }

            EditorGUILayout.LabelField("Workspaces", EditorStyles.boldLabel);
            if (mainMenu.menuWorkspaces != null)
            {
                foreach (var workspace in mainMenu.menuWorkspaces)
                {
                    if (GUILayout.Button(workspace.Name))
                        mainMenu.CreateWorkspace(workspace);
                }
            }
        }
    }
}
