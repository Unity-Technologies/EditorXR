using ListView;

public class FolderData : ListViewItemNestedData<FolderData>
{
	const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	readonly string m_Name;

	public int instanceID { get { return m_InstanceID; } }
	readonly int m_InstanceID;

	public AssetData[] assets { get { return m_Assets; } }
	readonly AssetData[] m_Assets;

	public FolderData(string name, FolderData[] children, AssetData[] assets, int instanceID, bool defaultToExpanded = false) : base(defaultToExpanded)
	{
		template = kTemplateName;
		m_Name = name;
		m_InstanceID = instanceID;
		this.children = children;
		m_Assets = assets;
	}
}