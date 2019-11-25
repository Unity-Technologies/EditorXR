using UnityEngine;

namespace Unity.Labs.EditorXR.Workspaces
{
    class MiniWorldUI : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Renderer m_Grid;

        [SerializeField]
        Transform m_BoundsCube;
#pragma warning restore 649

        public Renderer grid
        {
            get { return m_Grid; }
        }

        public Transform boundsCube
        {
            get { return m_BoundsCube; }
        }
    }
}
