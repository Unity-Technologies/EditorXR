using Unity.ListViewFramework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.EditorXR.Data
{
    sealed class AssetData : IListViewItemData<int>
    {
        public const string PrefabTypeString = "Prefab";
        public const string ModelTypeString = "Model";
        static readonly string k_TemplateName = "AssetGridItem";
        static readonly string k_GameObjetTypeString = "GameObject";

        public string template { get {return k_TemplateName; } }

        public int index { get; private set; }
        public string guid { get; private set; }
        public string name { get; private set; }
        public string type { get; private set; }
        public GameObject preview { get; set; }

        public bool selected => false;

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
            index = guid.GetHashCode();
            this.guid = guid;
            this.name = name;
            this.type = type;
        }

        void UpdateType()
        {
            if (type == k_GameObjetTypeString)
            {
#if UNITY_EDITOR
                switch (PrefabUtility.GetPrefabAssetType(asset))
                {
                    case PrefabAssetType.Model:
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
