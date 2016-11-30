using UnityEngine;

namespace ListView
{
	public class ListViewItem<DataType> : MonoBehaviour where DataType : ListViewItemData
	{
		public DataType data;

		public virtual void Setup(DataType data)
		{
			this.data = data;
		}
	}
}