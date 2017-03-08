#if UNITY_EDITOR
using ListView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Data
{
	sealed class AssetData : ListViewItemData<string>
	{
		const string k_TemplateName = "AssetGridItem";

		public string name
		{
			get { return m_Name; }
		}
		readonly string m_Name;

		public override string index
		{
			get { return m_Guid; }
		}
		readonly string m_Guid;

		public string type { get; private set; }

		public GameObject preview { get; set; }

		public Object asset
		{
			get { return m_Asset; }
			set
			{
				m_Asset = value;
				if (m_Asset)
					UpdateType(); // We lazy load assets and don't know the final type until the asset is loaded
			}
		}

		Object m_Asset;

		public AssetData(string name, string guid, string type)
		{
			template = k_TemplateName;
			m_Guid = guid;
			m_Name = name;
			this.type = type;
		}

		void UpdateType()
		{
			if (type == "GameObject")
			{
#if UNITY_EDITOR
				switch (PrefabUtility.GetPrefabType(asset))
				{
					case PrefabType.ModelPrefab:
						type = "Model";
						break;
					default:
						type = "Prefab";
						break;
				}
#endif
			}
		}
	}
}
#endif
