using UnityEngine;       

namespace ListView
{
    public abstract class ListViewInputHandler : MonoBehaviour
    {                              
        public ListViewControllerBase listView;

        void Update()
        {
            HandleInput();
        }

        protected abstract void HandleInput();
    }

    public abstract class ListViewScroller : ListViewInputHandler
    {
        protected bool m_Scrolling;
        protected Vector3 m_StartPosition;
        protected float m_StartOffset;

        protected virtual void StartScrolling(Vector3 start)
        {
            if (m_Scrolling)
                return;
            m_Scrolling = true;
            m_StartPosition = start;
            m_StartOffset = listView.scrollOffset;
        }

        protected virtual void Scroll(Vector3 position)
        {
            if (m_Scrolling)
                listView.scrollOffset = m_StartOffset + position.x - m_StartPosition.x;
        }

        protected virtual void StopScrolling()
        {
            m_Scrolling = false;
        }
    }
}