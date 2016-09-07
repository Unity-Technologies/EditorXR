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

	public AssetData(string name, int instanceID, Texture2D icon, string type)
	{
		template = kTemplateName;
		this.name = name;
		m_InstanceID = instanceID;
		m_Icon = icon;
		m_Type = type;
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