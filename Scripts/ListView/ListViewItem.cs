using UnityEngine;
using System.Collections.Generic;

namespace ListView
{
	public class ListViewItem : ListViewItem<ListViewItemInspectorData>
	{
	}
	
	public class ListViewItemBase : MonoBehaviour
	{
	}

	public class ListViewItem<DataType> : ListViewItemBase where DataType : ListViewItemData
	{
		public DataType data;

		public virtual void Setup(DataType data)
		{
			this.data = data;
			data.item = this;
		}
	}

	public class ListViewItemData
	{
		public string template;
		public MonoBehaviour item;
	}

	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public bool expanded { get; set; }
		public ChildType[] children { get; protected set; }
	}

	[System.Serializable]
	public class ListViewItemInspectorData : ListViewItemData
	{
	}

	public class ListViewItemTemplate
	{
		public readonly GameObject prefab;
		public readonly List<MonoBehaviour> pool = new List<MonoBehaviour>();

		public ListViewItemTemplate(GameObject prefab)
		{
			if (prefab == null)
				Debug.LogError("Template prefab cannot be null");
			this.prefab = prefab;
		}
	}
}