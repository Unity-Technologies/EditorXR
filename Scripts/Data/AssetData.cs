using System.IO;
using System.Linq;
using ListView;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";
	public string path { get { return m_Path; } }
	private string m_Path;
	private string m_Type;

	public string type { get { return m_Type; } }
	public bool animating { get; set; }

	public GameObject preview
	{
		get
		{
			if (!m_FetchedPreview)
				m_PreviewObject = GetAsset() as GameObject;
			m_FetchedPreview = true;
			return m_PreviewObject;
		}
	}
	private GameObject m_PreviewObject;
	private bool m_FetchedPreview;

	public AssetData(string path)
	{
		template = kTemplateName;
		m_Path = path;
		m_Type = GetTypeForAssetPath(path);
	}

	public static string GetPathRelativeToAssets(string path)
	{
		return path.Substring(path.IndexOf("Assets"));
	}

	public static AssetData[] GetAssetDataForPath(string path)
	{
		var paths = new DirectoryInfo(path).GetFiles()
					.Where(file => !file.Name.EndsWith(".meta") && !file.Name.StartsWith(".") && (file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden).ToArray();
		var files = new AssetData[paths.Length];
		for (int i = 0; i < paths.Length; i++)
		{
			files[i] = new AssetData(paths[i].FullName);
		}
		return files;
	}

	public Object GetAsset()
	{
		return AssetDatabase.LoadAssetAtPath(GetPathRelativeToAssets(m_Path), typeof(UnityEngine.Object));
	}

	public Texture GetCachedIcon()
	{
		return AssetDatabase.GetCachedIcon(GetPathRelativeToAssets(path));
	}

	public static string GetTypeForAssetPath(string path)
	{
		var importer = AssetImporter.GetAtPath(GetPathRelativeToAssets(path));
		if (importer != null)
		{
			var importerType = importer.GetType().Name.Replace("Importer", string.Empty);
			var extension = Path.GetExtension(path).ToLower();
			switch (importerType)
			{
				case "Asset":
					switch (extension)
					{
						case ".mat":
							return "Material";
						case ".anim":
							return "AnimationClip";
						case ".prefab":
							return "Prefab";
						case ".txt":
							return "Text";
						case ".controller":
							return "AnimationController";
						case ".unity":
							return "Scene";
						case ".mixer":
							return "AudioMixer";
					}
					break;
				case "Mono":
					return "Script";
				case "TrueTypeFont":
					return "Font";
			}
			return importerType;
		}
		return "Unknown";
	}
}