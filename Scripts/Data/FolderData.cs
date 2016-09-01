using System.IO;
using System.Linq;
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

	public bool selected { get; set; }

	public FolderData(string path, int depth = 0)
	{
		template = kTemplateName;
		m_Path = path;
		m_TreeDepth = depth;
		FolderData[] subFolders = GetFolderDataForPath(path, depth + 1);
		if (subFolders.Length > 0)
			children = subFolders;
		m_Assets = AssetData.GetAssetDataForPath(path);
	}

	public static FolderData[] GetFolderDataForPath(string path, int depth = 0)
	{
		//var dirs = new DirectoryInfo(path).GetDirectories().Where(dir => (dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).ToArray();
		var dirs = Directory.GetDirectories(path).Where(name => !Path.GetFileName(name).StartsWith(".")).ToArray();
		var files = new FolderData[dirs.Length];
		for (int i = 0; i < files.Length; i++)
		{
			files[i] = new FolderData(dirs[i], depth);
		}
		return files;
	}

	public void ClearSelected()
	{
		selected = false;
		if(children != null)
			foreach (var child in children)
				child.ClearSelected();
	}
}