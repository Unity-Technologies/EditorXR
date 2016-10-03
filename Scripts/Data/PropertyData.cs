using UnityEditor;

public class PropertyData : InspectorData
{
	public SerializedProperty property { get; private set; }

	public PropertyData(string template, SerializedObject serializedObject, InspectorData[] children, SerializedProperty property) : base(template, serializedObject, children)
	{
		this.property = property;
	}
}