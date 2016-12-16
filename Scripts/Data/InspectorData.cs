using ListView;
using System.Collections.Generic;
using UnityEditor;

public class InspectorData : ListViewItemNestedData<InspectorData>
{
#if UNITY_EDITOR
	public SerializedObject serializedObject { get; private set; }

	public virtual int instanceID
	{
		get { return serializedObject.targetObject.GetInstanceID(); }
	}

	public InspectorData(string template, SerializedObject serializedObject, List<InspectorData> children)
	{
		this.template = template;
		this.serializedObject = serializedObject;
		m_Children = children;
	}
#endif
}