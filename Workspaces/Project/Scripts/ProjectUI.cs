using UnityEngine;
using UnityEngine.VR.Handles;

public class ProjectUI : MonoBehaviour
{
	public FolderListViewController folderListView { get { return m_FolderListView; } }
	[SerializeField]
	private FolderListViewController m_FolderListView;

	public DirectHandle folderScrollHandle { get { return m_FolderScrollHandle; } }
	[SerializeField]
	private DirectHandle m_FolderScrollHandle;

	public RectTransform folderPanel { get { return m_FolderPanel; } }
	[SerializeField]
	private RectTransform m_FolderPanel;

	public AssetGridViewController assetListView { get { return m_AssetListView; } }
	[SerializeField]
	private AssetGridViewController m_AssetListView;

	public DirectHandle assetScrollHandle { get { return m_AssetScrollHandle; } }
	[SerializeField]
	private DirectHandle m_AssetScrollHandle;

	public RectTransform assetPanel { get { return m_AssetPanel; } }
	[SerializeField]
	private RectTransform m_AssetPanel;
}