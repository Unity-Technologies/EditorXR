using ListView;

public class InspectorData : ListViewItemNestedData<InspectorData>
{
	public string name { get; set; }

	public InspectorData()
	{
		template = "InspectorItem";
	}
}