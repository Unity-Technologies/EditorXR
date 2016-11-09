using ListView;

public class FolderData : ListViewItemNestedData<FolderData>
{
	private const string kTemplateName = "FolderListItem";

	public string name { get { return m_Name; } }
	private readonly string m_Name;

	public int instanceID { get { return m_InstanceID; } }
	readonly int m_InstanceID;

	public AssetData[] assets { get { return m_Assets; } }
	private readonly AssetData[] m_Assets;

	public bool selected { get; set; }

	public FolderData(string name, FolderData[] children, AssetData[] assets, int instanceID)
	{
		template = kTemplateName;
		m_Name = name;
		m_InstanceID = instanceID;
		this.children = children;
		m_Assets = assets;
	}

	public FolderData(FolderData original)
	{
		template = kTemplateName;
		m_Name = original.name;
		m_InstanceID = original.instanceID;

		if (original.children != null)
		{
			children = new FolderData[original.children.Length];
			for (var i = 0; i < children.Length; i++)
			{
				children[i] = new FolderData(original.children[i]);
			}
		}
		
		m_Assets = new AssetData[original.assets.Length];
		for (var i = 0; i < m_Assets.Length; i++)
		{
			m_Assets[i] = new AssetData(original.assets[i]);
		}
	}

	public void ClearSelected()
	{
		selected = false;
		if (children != null)
			foreach (var child in children)
				child.ClearSelected();
	}
}