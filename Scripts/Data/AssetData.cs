using ListView;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetData : ListViewItemData
{
	const string kTemplateName = "AssetGridItem";

	public string name { get; private set; }
	public string guid { get; private set; }

	public string type { get; private set; }

	public bool animating { get; set; }
	public GameObject preview { get; set; }

	public Object asset
	{
		get {
			return m_Asset;
		}
		set
		{
			m_Asset = value;
			if (m_Asset)
				CheckType();
		}
	}

	Object m_Asset;

	public AssetData(string name, string guid, string type)
	{
		template = kTemplateName;
		this.name = name;
		this.guid = guid;
		this.type = type;
	}

	/// <summary>
	/// In order to determine whether a GameObject is a model or a prefab, we need to load the asset.
	/// Then, we can narrow down the type.
	/// </summary>
	void CheckType()
	{
		if (type == "GameObject")
		{
			switch (PrefabUtility.GetPrefabType(asset))
			{
				case PrefabType.ModelPrefab:
					type = "Model";
					break;
				default:
					type = "Prefab";
					break;
			}

		}
	}
}