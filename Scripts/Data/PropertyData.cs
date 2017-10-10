#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class PropertyData : InspectorData
    {
#if UNITY_EDITOR
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
#endif
    }
}
#endif
