#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Experimental.EditorVR.Handles;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class InspectorUI : MonoBehaviour
	{
		public InspectorListViewController listView
		{
			get { return m_ListView; }
		}

		[SerializeField]
		InspectorListViewController m_ListView;

		public BaseHandle inspectorScrollHandle
		{
			get { return m_InspectorScrollHandle; }
		}

		[SerializeField]
		BaseHandle m_InspectorScrollHandle;

		public RectTransform inspectorPanel
		{
			get { return m_InspectorPanel; }
		}

		[SerializeField]
		RectTransform m_InspectorPanel;
	}
}
#endif
