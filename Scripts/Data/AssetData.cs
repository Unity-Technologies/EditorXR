using System.IO;
using ListView;

public class AssetData : ListViewItemNestedData<AssetData>
{
	private const string kTemplateName = "AssetItem";
	public string path {get { return m_Path;} }
	private string m_Path;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
		AssetData[] subFolders = GetAssetDataForPath(path);
		if (subFolders.Length > 0)
			children = subFolders;
	}

	public static AssetData[] GetAssetDataForPath(string path) {
		var paths = Directory.GetDirectories(path);
		var files = new AssetData[paths.Length];
		for (int i = 0; i < files.Length; i++) {
			files[i] = new AssetData(paths[i]);
		}
		return files;
	}
}