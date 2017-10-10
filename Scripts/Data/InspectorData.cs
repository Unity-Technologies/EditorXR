#if UNITY_EDITOR
using ListView;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Data
{
    class InspectorData : ListViewItemNestedData<InspectorData, int>
    {
#if UNITY_EDITOR
        public SerializedObject serializedObject { get; private set; }

        public InspectorData(string template, SerializedObject serializedObject, List<InspectorData> children)
        {
            this.template = template;
            this.serializedObject = serializedObject;
            index = serializedObject.targetObject.GetInstanceID();
            m_Children = children;
        }
#endif
    }
}
#endif
