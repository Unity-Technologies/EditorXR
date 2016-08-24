using System.IO;
using ListView;
using UnityEngine;

public class AssetData : ListViewItemNestedData<AssetData>
{
	private const string kTemplateName = "AssetItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public int treeDepth { get { return m_TreeDepth; } }
	private int m_TreeDepth;

	public AssetData(string path, int depth = 0)
	{
		template = kTemplateName;
		m_Path = path;
		m_TreeDepth = depth;
		AssetData[] subFolders = GetAssetDataForPath(path, depth + 1);
		if (subFolders.Length > 0)
			children = subFolders;
	}

	public static AssetData[] GetAssetDataForPath(string path, int depth = 0) {
		var paths = Directory.GetDirectories(path);
		var files = new AssetData[paths.Length];
		for (int i = 0; i < files.Length; i++) {
			files[i] = new AssetData(paths[i], depth);
		}
		return files;
	}
}