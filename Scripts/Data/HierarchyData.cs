using ListView;
using System.Collections.Generic;

public class HierarchyData : ListViewItemNestedData<HierarchyData>
{
	const string kTemplateName = "HierarchyListItem";

	public string name { get; set; }

	public int instanceID { get; set; }

	public HierarchyData(string name, int instanceID, List<HierarchyData> children = null)
	{
		template = kTemplateName;
		this.name = name;
		this.instanceID = instanceID;
		m_Children = children;
	}
}