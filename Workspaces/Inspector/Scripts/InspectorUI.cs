using UnityEngine;
using UnityEngine.VR.Handles;

public class InspectorUI : MonoBehaviour
{
	public InspectorListViewController inspectorListView { get { return m_InspectorListView; } }
	[SerializeField]
	private InspectorListViewController m_InspectorListView;

	public BaseHandle inspectorScrollHandle { get { return m_InspectorScrollHandle; } }
	[SerializeField]
	private BaseHandle m_InspectorScrollHandle;

	public RectTransform inspectorPanel { get { return m_InspectorPanel; } }
	[SerializeField]
	private RectTransform m_InspectorPanel;
}