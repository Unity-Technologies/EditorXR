using UnityEngine;

namespace UnityEditor
{
[CustomEditor(typeof(VRLineRenderer))]
public class VRLineRendererEditor : Editor
{
	public override void OnInspectorGUI()
	{
		// This is a bit of a brute force method to make this work in editor mode.
		// If we detect any kind of change, we just force a reinitialization
		// This way, nothing sneaks through from serialization
		base.OnInspectorGUI();
		if (GUI.changed)
		{
			var toUpdate = target as VRLineRenderer;
			toUpdate.EditorCheckForUpdate();
		}
	}
}
}