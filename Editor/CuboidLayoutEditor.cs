using UnityEditor;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;

[CustomEditor(typeof(CuboidLayout))]
public class CuboidLayoutEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var cuboid = (CuboidLayout)target;

		if (GUILayout.Button("Setup"))
		{
			cuboid.Setup();
		}

		if (GUILayout.Button("Update"))
		{
			cuboid.UpdateObjects();
		}
	}
}
