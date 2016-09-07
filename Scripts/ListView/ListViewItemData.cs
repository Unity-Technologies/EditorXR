using UnityEngine;

namespace ListView
{
	public class ListViewItemData
	{
		public string template { get; protected set; }
		public MonoBehaviour item { get; set; }
	}
}