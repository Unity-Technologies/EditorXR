using ListView;

public class NestedListViewController <DataType> : ListViewController<DataType, ListViewItem<DataType>> where DataType : ListViewItemNestedData<DataType> {

	protected override void UpdateItems() {
		int count = 0;
		UpdateRecursively(data, ref count);
	}

	void UpdateRecursively(DataType[] data, ref int count) {
		foreach (var item in data) {
			if (count + m_DataOffset < 0) {
				ExtremeLeft(item);
			} else if (count + m_DataOffset > m_NumItems) {
				ExtremeRight(item);
			} else {
				ListMiddle(item, count + m_DataOffset);
			}
			count++;
			if (item.children != null) {
				if (item.expanded) {
					UpdateRecursively(item.children, ref count);
				} else {
					RecycleChildren(item);
				}
			}
		}
	}

	void RecycleChildren(DataType data) {
		foreach (var child in data.children) {
			RecycleItem(child.template, child.item);
			child.item = null;
			if (child.children != null)
				RecycleChildren(child);
		}
	}
}