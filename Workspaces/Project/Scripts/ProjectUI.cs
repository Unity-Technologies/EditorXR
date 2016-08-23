using UnityEngine;
using System.Collections;

public class ProjectUI : MonoBehaviour
{
	public ProjectListViewController listView { get { return m_ListView; } }
	[SerializeField]
	private ProjectListViewController m_ListView;
}