using UnityEditor;

public class PropertyData : InspectorData
{
	public SerializedProperty property { get; private set; }
	public bool canToggleExpand { get; private set; }

	public PropertyData(string template, SerializedObject serializedObject, InspectorData[] children, SerializedProperty property, bool canToggleExpand = false) : base(template, serializedObject, children)
	{
		this.property = property;
		this.canToggleExpand = canToggleExpand;
	}
}