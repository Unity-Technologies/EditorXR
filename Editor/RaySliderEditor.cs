using UnityEditor;
using UnityEditor.UI;

namespace Unity.Labs.EditorXR.UI
{
    // Because Slider already has a custom editor, and we need to expose SelectionFlags, we need a custom inspector
    [CustomEditor(typeof(RaySlider))]
    sealed class RaySliderEditor : SliderEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelectionFlags"));
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}
