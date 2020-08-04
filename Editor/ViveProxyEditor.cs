using Unity.EditorXR.Proxies;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR
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
