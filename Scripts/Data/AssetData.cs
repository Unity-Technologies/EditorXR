using System.IO;
using ListView;
using UnityEngine;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
	}

	public static FolderData[] GetAssetDataForPath(string path) {
		var paths = Directory.GetDirectories(path);
		var files = new FolderData[paths.Length];
		for (int i = 0; i < files.Length; i++) {
			files[i] = new FolderData(paths[i]);
		}
		return files;
	}
}