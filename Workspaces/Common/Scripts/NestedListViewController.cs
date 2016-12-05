using System.Collections.Generic;

namespace ListView
{
	public class NestedListViewController<DataType> : ListViewController<DataType, ListViewItem<DataType>> where DataType : ListViewItemNestedData<DataType>
	{
		protected override int dataLength { get { return m_ExpandedDataLength; } }

		protected int m_ExpandedDataLength;

		protected void RecycleRecursively(DataType data)
		{
			Recycle(data);

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
			int count = 0;
			UpdateRecursively(m_Data, ref count);
			m_ExpandedDataLength = count;
		}

		protected virtual void UpdateRecursively(List<DataType> data, ref int count, int depth = 0)
		{
			foreach (var datum in data)
			{
				if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
					Recycle(datum);
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
				Recycle(child);

				if (child.children != null)
					RecycleChildren(child);
			}
		}
	}
}