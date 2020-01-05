using Unity.Labs.EditorXR.Menus;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    [CustomEditor(typeof(ToolsMenu))]
    class ToolsMenuEditor : Editor
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
}
