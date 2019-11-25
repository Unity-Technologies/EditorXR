using UnityEditor;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    [CustomEditor(typeof(ViveProxy))]
    class ViveProxyEditor : Editor
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
}
