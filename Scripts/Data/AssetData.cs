using System.IO;
using System.Linq;
using ListView;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";
	public string path { get { return m_Path; } }
	private string m_Path;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
	}

	public static AssetData[] GetAssetDataForPath(string path)
	{
		var paths = Directory.GetFiles(path).Where(name => !name.EndsWith(".meta")).ToArray();
		var files = new AssetData[paths.Length];
		for (int i = 0; i < paths.Length; i++)
		{
			files[i] = new AssetData(paths[i]);
		}
		return files;
	}
}