using System;
using UnityEditor;
using UnityEditor.Experimental.EditorVR.Input;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

public class DebugWindow : EditorWindow
{
    [MenuItem("Window/Debug")]
    static void Init()
    {
        GetWindow<DebugWindow>("Debug").Show();
    }

    void OnGUI()
    {
        foreach (var touchProxy in Resources.FindObjectsOfTypeAll<TouchProxy>())
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(touchProxy.name);
                GUILayout.Label((touchProxy.GetComponent<OVRTouchInputToEvents>() == null).ToString());
            }
        }
    }
}
