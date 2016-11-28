using ListView;

public class FolderData : ListViewItemNestedData<FolderData>
{
	const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	readonly string m_Name;

	public string guid { get { return m_Guid; } }
	readonly string m_Guid;

	public AssetData[] assets { get { return m_Assets; } }
	readonly AssetData[] m_Assets;

	public FolderData(string name, FolderData[] children, AssetData[] assets, string guid)
	{
		template = kTemplateName;
		m_Name = name;
		m_Guid = guid;
		this.children = children;
		m_Assets = assets;
	}
}