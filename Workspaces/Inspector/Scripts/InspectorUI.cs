using UnityEngine;
using UnityEngine.VR.Handles;

public class InspectorUI : MonoBehaviour
{
	public InspectorListViewController listView { get { return m_ListView; } }
	[SerializeField]
	InspectorListViewController m_ListView;

	public BaseHandle inspectorScrollHandle { get { return m_InspectorScrollHandle; } }
	[SerializeField]
	BaseHandle m_InspectorScrollHandle;

	public RectTransform inspectorPanel { get { return m_InspectorPanel; } }
	[SerializeField]
	RectTransform m_InspectorPanel;
}