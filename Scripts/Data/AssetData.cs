using System.Collections.Generic;
using ListView;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";

	public string type { get { return m_Type; } }
	private readonly string m_Type;
	private readonly int m_InstanceID;
	private readonly Texture2D m_Icon;

	public string name { get; private set; }
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

	public AssetData(HashSet<string> assetTypes, HierarchyProperty hp)
	{
		template = kTemplateName;
		m_InstanceID = hp.instanceID;
		m_Icon = hp.icon;
		name = hp.name;
		var type = hp.pptrValue.GetType().Name;
		switch (type)
		{
			case "GameObject":
				switch (PrefabUtility.GetPrefabType(GetAsset()))
				{
					case PrefabType.ModelPrefab:
						m_Type = "Model";
						break;
					default:
						m_Type = "Prefab";
						break;
				}
				break;
			case "MonoScript":
				m_Type = "Script";
				break;
			case "SceneAsset":
				m_Type = "Scene";
				break;
			case "AudioMixerController":
				m_Type = "AudioMixer";
				break;
			default:
				m_Type = type;
				break;
		}
		assetTypes.Add(m_Type);
	}

	public Object GetAsset()
	{
		return EditorUtility.InstanceIDToObject(m_InstanceID);
	}

	public Texture GetCachedIcon()
	{
		return m_Icon;
	}
}