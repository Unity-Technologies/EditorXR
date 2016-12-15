using UnityEngine;
using UnityEngine.Experimental.EditorVR.Handles;

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
}