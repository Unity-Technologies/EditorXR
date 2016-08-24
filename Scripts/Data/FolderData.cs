using System.IO;
using ListView;
using UnityEngine;

public class FolderData : ListViewItemNestedData<FolderData>
{
	private const string kTemplateName = "FolderListItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public int treeDepth { get { return m_TreeDepth; } }
	private int m_TreeDepth;

	public FolderData(string path, int depth = 0)
	{
		template = kTemplateName;
		m_Path = path;
		m_TreeDepth = depth;
		FolderData[] subFolders = GetAssetDataForPath(path, depth + 1);
		if (subFolders.Length > 0)
			children = subFolders;
	}

	public static FolderData[] GetAssetDataForPath(string path, int depth = 0) {
		var paths = Directory.GetDirectories(path);
		var files = new FolderData[paths.Length];
		for (int i = 0; i < files.Length; i++) {
			files[i] = new FolderData(paths[i], depth);
		}
		return files;
	}
}