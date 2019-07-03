using System.Collections.Generic;

#if !UNITY_EDITOR
class SerializedProperty
{
}
#endif

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class PropertyData : InspectorData
    {
        public SerializedProperty property { get; private set; }

        public PropertyData(string template, SerializedObject serializedObject, List<InspectorData> children, SerializedProperty property)
            : base(template, serializedObject, property.GetHashCode(), children)
        {
            this.property = property;
        }
    }
}
