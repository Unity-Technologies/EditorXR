using Unity.Labs.EditorXR.Interfaces;
using UnityEditor;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

[CustomEditor(typeof(MainMenu))]
public class MainMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var mainMenu = (MainMenu)target;

        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        foreach (var tool in mainMenu.menuTools)
        {
            if (GUILayout.Button(tool.Name))
                mainMenu.SelectTool(mainMenu.rayOrigin, tool);
        }

        EditorGUILayout.LabelField("Workspaces", EditorStyles.boldLabel);
        foreach (var workspace in mainMenu.menuWorkspaces)
        {
            if (GUILayout.Button(workspace.Name))
                mainMenu.CreateWorkspace(workspace);
        }
    }
}
