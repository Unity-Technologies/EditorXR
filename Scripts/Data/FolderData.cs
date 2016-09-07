using System.Collections.Generic;
using ListView;
using UnityEditor;

public class FolderData : ListViewItemNestedData<FolderData>
{
	private const string kTemplateName = "FolderListItem";
	public string name { get { return m_Name; } }
	private readonly string m_Name;

	public AssetData[] assets { get { return m_Assets; } }
	private readonly AssetData[] m_Assets;

	public bool selected { get; set; }

	public FolderData(HashSet<string> assetTypes, HierarchyProperty hp = null)
	{
		template = kTemplateName;
		if (hp == null)
		{
			hp = new HierarchyProperty(HierarchyType.Assets);
			hp.SetSearchFilter("t:object", 0);
		}
		m_Name = hp.name;
		var depth = hp.depth;
		var folderList = new List<FolderData>();
		var assetList = new List<AssetData>();
		while (hp.Next(null) && hp.depth > depth)
		{
			if (hp.isFolder)
				folderList.Add(new FolderData(assetTypes, hp));
			else if(hp.depth == depth + 1) // Ignore sub-assets (mixer children, terrain splats, etc.)
				assetList.Add(new AssetData(assetTypes, hp));
		}

		children = folderList.ToArray();
		m_Assets = assetList.ToArray();
	}

	public void ClearSelected()
	{
		selected = false;
		if (children != null)
			foreach (var child in children)
				child.ClearSelected();
	}
}