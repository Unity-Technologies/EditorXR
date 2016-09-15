using UnityEditor;

public class PropertyData : InspectorData
{
	public SerializedProperty property { get; private set; }

	public PropertyData(SerializedObject serializedObject, InspectorData[] children, bool canToggleExpand, SerializedProperty property) : base(serializedObject, children, canToggleExpand)
	{
		this.property = property;
	}
}