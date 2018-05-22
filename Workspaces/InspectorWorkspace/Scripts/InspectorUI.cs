
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

        public LinearHandle scrollHandle
        {
            get { return m_ScrollHandle; }
        }

        [SerializeField]
        LinearHandle m_ScrollHandle;

        public RectTransform listPanel
        {
            get { return m_ListPanel; }
        }

        [SerializeField]
        RectTransform m_ListPanel;
    }
}

