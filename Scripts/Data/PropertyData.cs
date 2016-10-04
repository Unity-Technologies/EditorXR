using System;
using UnityEditor;
using UnityEngine.VR.Utilities;

public class PropertyData : InspectorData
{
	public SerializedProperty property { get; private set; }
	public Action updateParent { get; private set; }

	public PropertyData(string template, SerializedObject serializedObject, InspectorData[] children, SerializedProperty property, Action updateParent = null)
		: base(template, serializedObject, children)
	{
		this.property = property;
		this.updateParent = updateParent;
	}

	public void SetChildren(InspectorData[] children)
	{
		foreach (var child in children)
		{
			if(child.item)
				U.Object.Destroy(child.item);
		}

		this.children = children;
	}
}