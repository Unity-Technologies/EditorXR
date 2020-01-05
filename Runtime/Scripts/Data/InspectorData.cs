using System.Collections.Generic;
using Unity.Labs.ListView;
using UnityEditor;

namespace Unity.Labs.EditorXR.Data
{
    class InspectorData : NestedListViewItemData<InspectorData, int>
    {
        readonly string m_Template;

        readonly int m_Index;

        public SerializedObject serializedObject { get; private set; }
        public override int index { get {return m_Index; } }
        public override string template { get { return m_Template; } }

        public InspectorData(string template, SerializedObject serializedObject, List<InspectorData> children)
            : this(template, serializedObject, serializedObject.targetObject.GetInstanceID(), children) { }

        protected InspectorData(string template, SerializedObject serializedObject, int index, List<InspectorData> children)
        {
            m_Template = template;
            this.serializedObject = serializedObject;
            m_Index = index;
            m_Children = children;
        }
    }
}
