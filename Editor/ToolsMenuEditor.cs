using UnityEditor;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEngine;

[CustomEditor(typeof(ToolsMenu))]
public class ToolsMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("FakeActivate"))
        {
            ((ToolsMenu)target).FakeActivate();
        }
    }
}
