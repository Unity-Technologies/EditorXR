using ListView;

public class HierarchyData : ListViewItemNestedData<HierarchyData>
{
	const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	readonly string m_Name;

	public int instanceID { get { return m_InstanceID; } }
	readonly int m_InstanceID;

	public bool selected { get; set; }

	public HierarchyData(string name, HierarchyData[] children, int instanceID)
	{
		template = kTemplateName;
		this.children = children;
		m_Name = name;
		m_InstanceID = instanceID;
	}

	public void ClearSelected()
	{
		selected = false;
		if (children != null)
			foreach (var child in children)
				child.ClearSelected();
	}
}