using Unity.EditorXR.Handles;
using UnityEngine;

namespace Unity.EditorXR.Workspaces
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
