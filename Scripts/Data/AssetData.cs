using System.IO;
using System.Linq;
using ListView;
using UnityEngine;

public class AssetData : ListViewItemNestedData<AssetData>
{
	private const string kTemplateName = "AssetItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
		FileAttributes attr = File.GetAttributes(path);
		if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
		{
			children = GetAssetDataForPath(path);
		}
	}

	public static AssetData[] GetAssetDataForPath(string path) {
		var paths = Directory.GetFileSystemEntries(path).Where(name => !name.EndsWith(".meta")).ToArray();
		var files = new AssetData[paths.Length];
		for (int i = 0; i < files.Length; i++) {
			files[i] = new AssetData(paths[i]);
		}
		return files;
	}
}