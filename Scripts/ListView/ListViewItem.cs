#if UNITY_EDITOR
using System;
using UnityEngine;

namespace ListView
{
	public class ListViewItem<DataType, IndexType> : MonoBehaviour where DataType : ListViewItemData<IndexType>
	{
		public DataType data { get; set; }
		public Action<Action> startSettling { protected get; set; }
		public Action endSettling { protected get; set; }
		public bool isStillSettling { protected set; get; }
		public Action<IndexType, Transform, bool> setRowGrabbed { protected get; set; }
		public Func<Transform, ListViewItem<DataType, IndexType>> getGrabbedRow { protected get; set; }
		public Func<IndexType, ListViewItem<DataType, IndexType>> getListItem { protected get; set; }

		public virtual void Setup(DataType data)
		{
			this.data = data;
		}
	}
}
#endif
