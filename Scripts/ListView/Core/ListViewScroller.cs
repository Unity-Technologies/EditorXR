using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.Labs.ListView
{
    public abstract class ListViewScroller : MonoBehaviour, IScrollHandler
    {
        [SerializeField]
        protected ListViewControllerBase m_ListView;

        protected bool m_Scrolling;
        protected Vector3 m_StartPosition;
        protected float m_StartOffset;

        protected virtual void OnScrollStarted(Vector3 start)
        {
            if (m_Scrolling)
                return;

            m_Scrolling = true;
            m_StartPosition = start;
            m_StartOffset = m_ListView.scrollOffset;
            m_ListView.OnScrollStarted();
        }

        protected virtual void OnScroll(Vector3 position)
        {
            if (m_Scrolling)
                m_ListView.scrollOffset = m_StartOffset + position.x - m_StartPosition.x;
        }

        protected virtual void OnScrollEnded()
        {
            m_Scrolling = false;
            m_ListView.OnScrollEnded();
        }

        public void OnScroll(PointerEventData eventData)
        {
            m_ListView.OnScroll(eventData);
        }
    }
}
