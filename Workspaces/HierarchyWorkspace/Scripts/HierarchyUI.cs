
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class HierarchyUI : MonoBehaviour
    {
        [SerializeField]
        HierarchyListViewController m_ListView;

        [SerializeField]
        BaseHandle m_ScrollHandle;

        public HierarchyListViewController listView { get { return m_ListView; } }

        public BaseHandle scrollHandle { get { return m_ScrollHandle; } }
    }
}

