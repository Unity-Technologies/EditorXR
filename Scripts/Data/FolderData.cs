using System.IO;
using ListView;

public class FolderData : ListViewItemNestedData<FolderData>
{
	private const string kTemplateName = "FolderListItem";
	public string path { get { return m_Path; } }
	private string m_Path;

	public int treeDepth { get { return m_TreeDepth; } }
	private int m_TreeDepth;

	public AssetData[] assets { get { return m_Assets; } }
	private AssetData[] m_Assets;

	public FolderData(string path, int depth = 0)
	{
		template = kTemplateName;
		m_Path = path;
		m_TreeDepth = depth;
		FolderData[] subFolders = GetFolderDataForPath(path, depth + 1);
		if (subFolders.Length > 0)
			children = subFolders;
		var assets = AssetData.GetAssetDataForPath(path);
		if (assets.Length > 0)
			m_Assets = assets;
	}

	public static FolderData[] GetFolderDataForPath(string path, int depth = 0)
	{
		var paths = Directory.GetDirectories(path);
		//var paths = AssetDatabase.GetSubFolders(path);
		var files = new FolderData[paths.Length];
		for (int i = 0; i < files.Length; i++)
		{
			files[i] = new FolderData(paths[i], depth);
		}
		return files;
	}
}