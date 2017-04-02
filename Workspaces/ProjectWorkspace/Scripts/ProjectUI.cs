#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class ProjectUI : MonoBehaviour
	{
		public FolderListViewController folderListView { get { return m_FolderListView; } }
		[SerializeField]
		FolderListViewController m_FolderListView;

		public LinearHandle folderScrollHandle { get { return m_FolderScrollHandle; } }
		[SerializeField]
		LinearHandle m_FolderScrollHandle;

		public RectTransform folderPanel { get { return m_FolderPanel; } }
		[SerializeField]
		RectTransform m_FolderPanel;

		public AssetGridViewController assetGridView { get { return m_AssetGridView; } }
		[SerializeField]
		AssetGridViewController m_AssetGridView;

		public LinearHandle assetScrollHandle { get { return m_AssetScrollHandle; } }
		[SerializeField]
		LinearHandle m_AssetScrollHandle;

		public RectTransform assetPanel { get { return m_AssetPanel; } }
		[SerializeField]
		RectTransform m_AssetPanel;

		public WorkspaceHighlight assetGridHighlight { get { return m_AssetGridHighlight; } }
		[SerializeField]
		WorkspaceHighlight m_AssetGridHighlight;

		public WorkspaceHighlight folderPanelHighlight { get { return m_FolderPanelHighlight; } }
		[SerializeField]
		WorkspaceHighlight m_FolderPanelHighlight;
	}
}
#endif
