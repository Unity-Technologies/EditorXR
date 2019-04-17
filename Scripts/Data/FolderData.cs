using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class FolderData : EditorXRListViewItemNestedData<FolderData, int>
    {
        const string k_TemplateName = "FolderListItem";

        public string name { get; private set; }

        public List<AssetData> assets
        {
            get { return m_Assets; }
        }

        readonly List<AssetData> m_Assets;

        public FolderData(string name, List<FolderData> children, List<AssetData> assets, int guid)
        {
            template = k_TemplateName;
            this.name = name;
            index = guid;
            m_Children = children;
            m_Assets = assets;
        }
    }
}
