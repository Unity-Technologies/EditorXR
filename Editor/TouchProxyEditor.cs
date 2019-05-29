using UnityEditor;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

[CustomEditor(typeof(TouchProxy))]
public class TouchProxyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("FakeActivate"))
        {
            ((TouchProxy)target).FakeActivate();
        }
    }
}
