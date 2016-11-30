using ListView;
using System.Collections.Generic;

public class FolderData : ListViewItemNestedData<FolderData>
{
	const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	readonly string m_Name;

	public string guid { get { return m_Guid; } }
	readonly string m_Guid;

	public List<AssetData> assets { get { return m_Assets; } }
	readonly List<AssetData> m_Assets;

	public FolderData(string name, List<FolderData> children, List<AssetData> assets, string guid)
	{
		template = kTemplateName;
		m_Name = name;
		m_Guid = guid;
		m_Children = children;
		m_Assets = assets;
	}
}