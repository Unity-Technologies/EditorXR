#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class BlocksUI : MonoBehaviour
    {
        [SerializeField]
        BlocksGridViewController m_GridView;

        [SerializeField]
        LinearHandle m_ScrollHandle;

        public BlocksGridViewController gridView { get { return m_GridView; } }
        public LinearHandle scrollHandle { get { return m_ScrollHandle; } }
    }
}
#endif
