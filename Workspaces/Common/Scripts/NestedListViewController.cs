namespace ListView
{
	public class NestedListViewController<DataType> : ListViewController<DataType, ListViewItem<DataType>> where DataType : ListViewItemNestedData<DataType>
	{
		protected override int dataLength { get { return m_ExpandedDataLength; } }

		protected int m_ExpandedDataLength;

		protected override void UpdateItems()
		{
			int count = 0;
			UpdateRecursively(m_Data, ref count);
			m_ExpandedDataLength = count;
		}

		protected virtual void UpdateRecursively(DataType[] data, ref int count)
		{
			foreach (var item in data)
			{
				if (count + m_DataOffset < -1)
				{
					CleanUpBeginning(item);
				}
				else if (count + m_DataOffset > m_NumRows - 1)
				{
					CleanUpEnd(item);
				}
				else
				{
					UpdateVisibleItem(item, count);
				}
				count++;
				if (item.children != null)
				{
					if (item.expanded)
					{
						UpdateRecursively(item.children, ref count);
					}
					else
					{
						RecycleChildren(item);
					}
				}
			}
		}

		protected void RecycleChildren(DataType data)
		{
			foreach (var child in data.children)
			{
				RecycleItem(child.template, child.item);
				child.item = null;
				if (child.children != null)
					RecycleChildren(child);
			}
		}
	}
}