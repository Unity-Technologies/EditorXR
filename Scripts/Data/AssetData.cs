using ListView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class AssetData : ListViewItemData<int>
    {
        public const string PrefabTypeString = "Prefab";
        public const string ModelTypeString = "Model";
        static readonly string k_TemplateName = "AssetGridItem";

        public string guid { get; private set; }

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
            index = guid.GetHashCode();
            this.guid = guid;
            this.name = name;
            this.type = type;
        }

        void UpdateType()
        {
            if (type == "GameObject")
            {
#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
                switch (PrefabUtility.GetPrefabAssetType(asset))
#else
                switch (PrefabUtility.GetPrefabType(asset))
#endif
                {
#if UNITY_2018_3_OR_NEWER
                    case PrefabAssetType.Model:
#else
                    case PrefabType.ModelPrefab:
#endif
                        type = ModelTypeString;
                        break;
                    default:
                        type = PrefabTypeString;
                        break;
                }
#endif
            }
        }
    }
}
