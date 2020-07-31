using Unity.EditorXR.Menus;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR
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
