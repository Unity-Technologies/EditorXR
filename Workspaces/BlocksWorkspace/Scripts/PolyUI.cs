#if UNITY_EDITOR
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class PolyUI : MonoBehaviour
    {
        [SerializeField]
        PolyGridViewController m_GridView;

        [SerializeField]
        LinearHandle m_ScrollHandle;

        public PolyGridViewController gridView { get { return m_GridView; } }
        public LinearHandle scrollHandle { get { return m_ScrollHandle; } }
    }
}
#endif
