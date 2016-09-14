using ListView;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetData : ListViewItemData
{
	private const string kTemplateName = "AssetGridItem";

	public string type { get { return m_Type; } }
	private readonly string m_Type;
	
	private readonly Texture2D m_Icon;

	public int instanceID { get; private set; }

	public string name { get; private set; }
	public bool animating { get; set; }
	public Object asset { get; set; }
	public GameObject preview { get; set; }

	public AssetData(string name, int instanceID, Texture2D icon, string type)
	{
		template = kTemplateName;
		this.name = name;
		this.instanceID = instanceID;
		m_Icon = icon;
		m_Type = type;
	}

	public Texture GetCachedIcon()
	{
		return m_Icon;
	}
}