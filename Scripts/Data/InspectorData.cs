using System;
using ListView;
using UnityEditor;

public class InspectorData : ListViewItemNestedData<InspectorData>
{
	public SerializedObject serializedObject { get; private set; }

	public virtual int instanceID
	{
		get { return serializedObject.targetObject.GetInstanceID(); }
	}

	public InspectorData(string template, SerializedObject serializedObject, InspectorData[] children)
	{
		this.template = template;
		this.serializedObject = serializedObject;
		this.children = children;
	}
}