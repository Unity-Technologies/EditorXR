using ListView;
using UnityEditor;

public class InspectorData : ListViewItemNestedData<InspectorData>
{
	public SerializedObject serializedObject { get; private set; }
	public bool canToggleExpand { get; private set; }

	public InspectorData(SerializedObject serializedObject, InspectorData[] children, bool canToggleExpand = false)
	{
		template = "InspectorItem";
		this.serializedObject = serializedObject;
		this.children = children;
		this.canToggleExpand = canToggleExpand;
	}
}