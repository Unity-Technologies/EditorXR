
using ListView;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListViewScroller : MonoBehaviour, IScrollHandler
{
    [SerializeField]
    ListViewControllerBase m_ListView;

    public void OnScroll(PointerEventData eventData)
    {
        m_ListView.OnScroll(eventData);
    }
}

