using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
class LockableHierarchyGUI : ScriptableSingleton<LockableHierarchyGUI>
{
#pragma warning disable 649
    [SerializeField]
    Texture2D m_LockIcon;

    [SerializeField]
    Texture2D m_UnlockIcon;
#pragma warning restore 649

    static LockableHierarchyGUI()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemGUI;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
    }

    static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
    {
        Rect r = new Rect(selectionRect);
        var iconSize = r.height;
        r.x = r.xMax - iconSize;
        r.width = iconSize;

        var e = Event.current;
        var mousePosition = e.mousePosition;

        var window = EditorWindow.mouseOverWindow;

        // Normally the HierarchyWindow doesn't repaint often, so this helps improve the responsiveness
        if (window && mousePosition.x >= 0 && mousePosition.y >= 0
            && mousePosition.x <= window.position.width
            && mousePosition.y <= window.position.height)
        {
            window.wantsMouseMove = true;
            window.Repaint();
        }

        var go = EditorUtility.InstanceIDToObject(instanceID);
        if (go)
        {
            Texture2D icon = null;
            if ((go.hideFlags & HideFlags.NotEditable) != 0)
            {
                icon = r.Contains(mousePosition) ? instance.m_UnlockIcon : instance.m_LockIcon;
            }
            else if (selectionRect.Contains(mousePosition))
            {
                icon = instance.m_LockIcon;
                GUI.color = Color.grey;
            }

            if (GUI.Button(r, icon, EditorStyles.label))
            {
                go.hideFlags ^= HideFlags.NotEditable;
                EditorUtility.SetDirty(go);
            }
        }

        GUI.color = Color.white;
    }
}
