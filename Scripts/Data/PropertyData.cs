using System.Collections.Generic;

#if !UNITY_EDITOR
        class SerializedProperty {}
#endif

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class PropertyData : InspectorData
    {
        public SerializedProperty property { get; private set; }

        public override int index
        {
            get { return property.GetHashCode(); }
        }

        public PropertyData(string template, SerializedObject serializedObject, List<InspectorData> children, SerializedProperty property)
            : base(template, serializedObject, children)
        {
            this.property = property;
        }

    }
}
