using Unity.Labs.EditorXR.Handles;
using UnityEngine;

namespace Unity.Labs.EditorXR.Workspaces
{
    sealed class ProjectUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        FolderListViewController m_FolderListView;

        [SerializeField]
        LinearHandle m_FolderScrollHandle;

        [SerializeField]
        AssetGridViewController m_AssetGridView;

        [SerializeField]
        LinearHandle m_AssetScrollHandle;

        [SerializeField]
        WorkspaceHighlight m_AssetGridHighlight;

        [SerializeField]
        WorkspaceHighlight m_FolderPanelHighlight;
#pragma warning restore 649

        public FolderListViewController folderListView { get { return m_FolderListView; } }
        public LinearHandle folderScrollHandle { get { return m_FolderScrollHandle; } }
        public AssetGridViewController assetGridView { get { return m_AssetGridView; } }
        public LinearHandle assetScrollHandle { get { return m_AssetScrollHandle; } }
        public WorkspaceHighlight assetGridHighlight { get { return m_AssetGridHighlight; } }
        public WorkspaceHighlight folderPanelHighlight { get { return m_FolderPanelHighlight; } }
    }
}
