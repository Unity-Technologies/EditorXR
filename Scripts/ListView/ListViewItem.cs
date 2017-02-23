using UnityEngine;

namespace ListView
{
	public class ListViewItem<DataType, IndexType> : MonoBehaviour where DataType : ListViewItemData<IndexType>
	{
		public DataType data;

		public virtual void Setup(DataType data)
		{
			this.data = data;
		}
	}
}