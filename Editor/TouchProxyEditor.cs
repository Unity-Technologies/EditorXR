using Unity.Labs.EditorXR.Proxies;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.EditorXR
{
    [CustomEditor(typeof(TouchProxy))]
    class TouchProxyEditor : Editor
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
}
