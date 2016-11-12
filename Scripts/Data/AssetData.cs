using ListView;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";

	public string name { get; private set; }
	public int instanceID { get; private set; }

	public string type { get; private set; }

	public bool animating { get; set; }
	public Object asset { get; set; }
	public GameObject preview { get; set; }

	public AssetData(string name, int instanceID, string type)
	{
		template = kTemplateName;
		this.name = name;
		this.instanceID = instanceID;
		this.type = type;
	}

	public AssetData(AssetData original)
	{
		template = kTemplateName;
		name = original.name;
		instanceID = original.instanceID;
		type = original.type;
	}
}