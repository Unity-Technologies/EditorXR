using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.UI;

public class ProjectUI : MonoBehaviour
{
	public FolderListViewController folderListView { get { return m_FolderListView; } }
	[SerializeField]
	private FolderListViewController m_FolderListView;

	public BaseHandle folderScrollHandle { get { return m_FolderScrollHandle; } }
	[SerializeField]
	private BaseHandle m_FolderScrollHandle;

	public RectTransform folderPanel { get { return m_FolderPanel; } }
	[SerializeField]
	private RectTransform m_FolderPanel;

	public AssetGridViewController assetGridView { get { return m_AssetGridView; } }
	[SerializeField]
	private AssetGridViewController m_AssetGridView;

	public BaseHandle assetScrollHandle { get { return m_AssetScrollHandle; } }
	[SerializeField]
	private BaseHandle m_AssetScrollHandle;

	public RectTransform assetPanel { get { return m_AssetPanel; } }
	[SerializeField]
	private RectTransform m_AssetPanel;

	public WorkspaceHighlight assetGridHighlight { get { return m_AssetGridHighlight; } }
	[SerializeField]
	WorkspaceHighlight m_AssetGridHighlight;

	public WorkspaceHighlight folderPanelHighlight { get { return m_FolderPanelHighlight; } }
	[SerializeField]
	WorkspaceHighlight m_FolderPanelHighlight;
}