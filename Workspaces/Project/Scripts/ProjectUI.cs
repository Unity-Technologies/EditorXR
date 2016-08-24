using UnityEngine;
using System.Collections;
using UnityEngine.VR.Handles;

public class ProjectUI : MonoBehaviour
{
	public FolderListViewController listView { get { return m_ListView; } }
	[SerializeField]
	private FolderListViewController m_ListView;

	public DirectHandle folderScrollHandle { get { return m_FolderScrollHandle; } }
	[SerializeField]
	private DirectHandle m_FolderScrollHandle;

	public DirectHandle assetScrollHandle { get { return m_AssetScrollHandle; } }
	[SerializeField]
	private DirectHandle m_AssetScrollHandle;
}