using ListView;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListViewScroller : MonoBehaviour, IScrollHandler
{
#pragma warning disable 649
    [SerializeField]
    ListViewControllerBase m_ListView;
#pragma warning restore 649

    public void OnScroll(PointerEventData eventData)
    {
        m_ListView.OnScroll(eventData);
    }
}
