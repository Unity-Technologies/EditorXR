#if UNITY_EDITOR
using System.Collections.Generic;

namespace ListView
{
	class NestedListViewController<DataType, IndexType> 
		: ListViewController<DataType, ListViewItem<DataType, IndexType>, IndexType>
		where DataType : ListViewItemNestedData<DataType, IndexType>
	{

		protected override int dataLength { get { return m_ExpandedDataLength; } }

		protected int m_ExpandedDataLength;

		protected readonly Dictionary<IndexType, bool> m_ExpandStates = new Dictionary<IndexType, bool>();

		public override List<DataType> data
		{
			get { return base.data; }
			set
			{
				m_Data = value;

				// Update visible rows
				foreach (var row in m_ListItems)
				{
					var newData = GetRowRecursive(m_Data, row.Key);
					if (newData != null)
						row.Value.Setup(newData);
				}
			}
		}

		static DataType GetRowRecursive(List<DataType> data, IndexType index)
		{
			foreach (var datum in data)
			{
				if (datum.index.Equals(index))
					return datum;

				if (datum.children != null)
				{
					var result = GetRowRecursive(datum.children, index);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		protected void RecycleRecursively(DataType data)
		{
			Recycle(data.index);

			if (data.children != null)
			{
				foreach (var child in data.children)
				{
					RecycleRecursively(child);
				}
			}
		}

		protected override void UpdateItems()
		{
			m_SettleTest = true;
			var count = 0;
			UpdateRecursively(m_Data, ref count);
			m_ExpandedDataLength = count;
			if (m_Settling && m_SettleTest)
				EndSettling();
		}

		protected virtual void UpdateRecursively(List<DataType> data, ref int count, int depth = 0)
		{
			foreach (var datum in data)
			{
				if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
					Recycle(datum.index);
				else
					UpdateNestedItem(datum, count, depth);

				count++;

				if (datum.children != null)
					UpdateRecursively(datum.children, ref count, depth + 1);
			}
		}

		protected virtual void UpdateNestedItem(DataType data, int count, int depth)
		{
			UpdateVisibleItem(data, count);
		}

		protected void RecycleChildren(DataType data)
		{
			foreach (var child in data.children)
			{
				Recycle(child.index);

				if (child.children != null)
					RecycleChildren(child);
			}
		}
	}
}
#endif
