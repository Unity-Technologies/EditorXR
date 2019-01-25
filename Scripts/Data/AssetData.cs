using ListView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class AssetData : ListViewItemData<string>
    {
        const string k_TemplateName = "AssetGridItem";

        public string name { get; private set; }

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
            index = guid;
            this.name = name;
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
