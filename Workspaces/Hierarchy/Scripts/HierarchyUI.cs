using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.UI;

public class HierarchyUI : MonoBehaviour
{
	public HierarchyListViewController hierarchyListView { get { return m_HierarchyListView; } }
	[SerializeField]
	HierarchyListViewController m_HierarchyListView;

	public BaseHandle scrollHandle { get { return m_ScrollHandle; } }
	[SerializeField]
	BaseHandle m_ScrollHandle;

	public WorkspaceHighlight highlight { get { return m_Highlight; } }
	[SerializeField]
	WorkspaceHighlight m_Highlight;
}