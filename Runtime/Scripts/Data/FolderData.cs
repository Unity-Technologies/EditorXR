using System.Collections.Generic;
using Unity.Labs.ListView;

namespace UnityEditor.Experimental.EditorVR.Data
{
    sealed class FolderData : NestedListViewItemData<FolderData, int>
    {
        const string k_TemplateName = "FolderListItem";

        readonly List<AssetData> m_Assets;
        readonly int m_Index;

        public string name { get; private set; }
        public List<AssetData> assets { get { return m_Assets; } }
        public override int index { get { return m_Index; } }
        public override string template { get { return k_TemplateName; } }

        public FolderData(string name, List<FolderData> children, List<AssetData> assets, int guid)
        {
            this.name = name;
            m_Index = guid;
            m_Children = children;
            m_Assets = assets;
        }
    }
}
