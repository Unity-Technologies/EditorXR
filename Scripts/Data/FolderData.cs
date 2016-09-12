using ListView;

public class FolderData : ListViewItemNestedData<FolderData>
{
	private const string kTemplateName = "FolderListItem";
	public string name { get { return m_Name; } }
	private readonly string m_Name;

	public AssetData[] assets { get { return m_Assets; } }
	private readonly AssetData[] m_Assets;

	public bool selected { get; set; }

	public FolderData(string name, FolderData[] children, AssetData[] assets)
	{
		template = kTemplateName;
		m_Name = name;
		this.children = children;
		m_Assets = assets;
	}

	public void ClearSelected()
	{
		selected = false;
		if (children != null)
			foreach (var child in children)
				child.ClearSelected();
	}
}