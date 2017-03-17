using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
class LockedHierarchyIcon : ScriptableObject
{
	[SerializeField]
	Texture2D m_LockIcon;

	[SerializeField]
	Texture2D m_UnlockIcon;

	static LockedHierarchyIcon()
	{
		EditorApplication.delayCall += () => CreateInstance<LockedHierarchyIcon>();
	}

	void OnEnable()
	{
		EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
	}

	void OnDisable()
	{
		EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemGUI;
	}

	void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
	{
		const float iconSize = 18f;
		Rect r = new Rect(selectionRect);
		r.x = r.xMax - iconSize;
		r.width = iconSize;
		r.height = iconSize;

		var e = Event.current;
		var mousePosition = e.mousePosition;

		var window = EditorWindow.mouseOverWindow;
		// Normally the HierarchyWindow doesn't repaint often, so this helps improve the responsiveness
		if (window && mousePosition.x >= 0 && mousePosition.y >= 0
			&& mousePosition.x <= window.position.width 
			&& mousePosition.y <= window.position.height)
		{
			window.Repaint();
		}

		var go = EditorUtility.InstanceIDToObject(instanceID);
		if (go)
		{
			Texture2D icon = null;
			if ((go.hideFlags & HideFlags.NotEditable) != 0)
			{
				icon = r.Contains(mousePosition) ? m_UnlockIcon : m_LockIcon;
			}
			else if (selectionRect.Contains(mousePosition))
			{
				icon = m_LockIcon;
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