using ListView;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListViewScroller : MonoBehaviour, IScrollHandler
{
	[SerializeField]
	ListViewControllerBase listView;

	public void OnScroll(PointerEventData eventData)
	{
		listView.OnScroll(eventData);
	}
}
