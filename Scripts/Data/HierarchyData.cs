using ListView;

public class HierarchyData : ListViewItemNestedData<HierarchyData>
{
	const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	readonly string m_Name;

	public int instanceID { get { return m_InstanceID; } }
	readonly int m_InstanceID;

	public HierarchyData(string name, int instanceID)
	{
		template = kTemplateName;
		m_Name = name;
		m_InstanceID = instanceID;
	}

	public HierarchyData(string name, int instanceID, HierarchyData[] children) : this(name, instanceID)
	{
		m_Children = children;
	}
}