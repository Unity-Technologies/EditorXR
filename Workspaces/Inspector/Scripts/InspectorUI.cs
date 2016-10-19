using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.UI;

public class InspectorUI : MonoBehaviour
{
	public InspectorListViewController inspectorListView { get { return m_InspectorListView; } }
	[SerializeField]
	InspectorListViewController m_InspectorListView;

	public BaseHandle inspectorScrollHandle { get { return m_InspectorScrollHandle; } }
	[SerializeField]
	BaseHandle m_InspectorScrollHandle;

	public RectTransform inspectorPanel { get { return m_InspectorPanel; } }
	[SerializeField]
	RectTransform m_InspectorPanel;

	public WorkspaceHighlight highlight { get { return m_highlight; } }
	[SerializeField]
	WorkspaceHighlight m_highlight;
}