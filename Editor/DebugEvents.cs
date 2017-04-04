using UnityEditor;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

public class DebugEvents : EditorWindow
{
	[MenuItem("Window/Debug")]
	static void Init()
	{
		Debug.Log(typeof(DebugEvents).FullName);
		var window = GetWindow<DebugEvents>();
		window.Show();
	}

	void OnGUI()
	{
		GUILayout.Label(ConsoleWorkspace.mousePosition.ToString());

		GUI.Box(position, "");
		GUI.Box(new Rect(ConsoleWorkspace.mousePosition, new Vector2(1,1)), "");
	}
}