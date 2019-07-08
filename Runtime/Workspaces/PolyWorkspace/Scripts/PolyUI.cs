using UnityEditor.Experimental.EditorVR.Handles;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class PolyUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        PolyGridViewController m_GridView;

        [SerializeField]
        LinearHandle m_ScrollHandle;
#pragma warning restore 649

        public PolyGridViewController gridView { get { return m_GridView; } }
        public LinearHandle scrollHandle { get { return m_ScrollHandle; } }
    }
}
