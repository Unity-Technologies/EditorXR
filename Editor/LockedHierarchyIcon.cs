using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
class LockedHierarchyIcon : ScriptableObject
{
	[SerializeField]
	Texture2D m_Icon;

	//static List<int> markedObjects;

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
		// place the icoon to the right of the list:
		const float iconSize = 18f;
		Rect r = new Rect(selectionRect);
		r.x = r.xMax - iconSize;
		r.width = iconSize;
		r.height = iconSize;
		
		var go = EditorUtility.InstanceIDToObject(instanceID);
		if (go)
		{
			var icon = (go.hideFlags & HideFlags.NotEditable) != 0 ? m_Icon : null;
			if (GUI.Button(r, icon, EditorStyles.label))
			{
				go.hideFlags ^= HideFlags.NotEditable;
				EditorUtility.SetDirty(go);
			}
		}
	}
}