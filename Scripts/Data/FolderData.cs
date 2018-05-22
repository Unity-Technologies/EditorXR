#if UNITY_EDITOR
using ListView;
using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class FolderData : ListViewItemNestedData<FolderData, string>
    {
        const string k_TemplateName = "FolderListItem";

        public string name { get; private set; }

        public List<AssetData> assets
        {
            get { return m_Assets; }
        }

        readonly List<AssetData> m_Assets;

        public FolderData(string name, List<FolderData> children, List<AssetData> assets, string guid)
        {
            template = k_TemplateName;
            this.name = name;
            index = guid;
            m_Children = children;
            m_Assets = assets;
        }
    }
}
#endif