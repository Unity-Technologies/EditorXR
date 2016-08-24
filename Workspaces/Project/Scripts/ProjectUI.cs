using UnityEngine;
using System.Collections;
using UnityEngine.VR.Handles;

public class ProjectUI : MonoBehaviour
{
	public ProjectListViewController listView { get { return m_ListView; } }
	[SerializeField]
	private ProjectListViewController m_ListView;

	public DirectHandle scrollHandle { get { return m_ScrollHandle; } }
	[SerializeField]
	private DirectHandle m_ScrollHandle;
}