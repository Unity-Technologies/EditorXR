using UnityEditor;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

[CustomEditor(typeof(ViveProxy))]
public class ViveProxyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("FakeActivate"))
        {
            ((ViveProxy)target).FakeActivate();
        }
    }
}
