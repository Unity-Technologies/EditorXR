#if UNITY_EDITOR
using System.Collections.Generic;

namespace ListView
{
	class NestedListViewController<TData, TItem, TIndex> 
		: ListViewController<TData, TItem, TIndex>
		where TData : ListViewItemNestedData<TData, TIndex>
		where TItem : ListViewItem<TData, TIndex>
	{

		protected override float listHeight { get { return m_ExpandedDataLength; } }

		protected float m_ExpandedDataLength;

		protected readonly Dictionary<TIndex, bool> m_ExpandStates = new Dictionary<TIndex, bool>();

		public override List<TData> data
		{
			get { return base.data; }
			set
			{
				m_Data = value;

				// Update visible rows
				var items = new Dictionary<TIndex, TItem>(m_ListItems);
				foreach (var row in items)
				{
					var index = row.Key;
					var newData = GetRowRecursive(m_Data, index);
					if (newData != null)
						row.Value.Setup(newData);
					else
						Recycle(index);
				}
			}
		}

		static TData GetRowRecursive(List<TData> data, TIndex index)
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

		protected void RecycleRecursively(TData data)
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
			var count = 0f;
			UpdateRecursively(m_Data, ref count);
			m_ExpandedDataLength = count;
			if (m_Settling && m_SettleTest)
				EndSettling();
		}

		protected virtual void UpdateRecursively(List<TData> data, ref float offset, int depth = 0)
		{
			for (int i = 0; i < data.Count; i++)
			{
				var datum = data[i];

				var index = datum.index;
				bool expanded;
				if (!m_ExpandStates.TryGetValue(index, out expanded))
					m_ExpandStates[index] = false;

				var itemSize = m_ItemSize.Value;

				if (offset + scrollOffset + itemSize.z < 0 || offset + scrollOffset > bounds.size.z)
					Recycle(index);
				else
					UpdateNestedItem(datum, offset, depth);

				offset += itemSize.z;

				if (datum.children != null)
				{
					if (expanded)
						UpdateRecursively(datum.children, ref offset, depth + 1);
					else
						RecycleChildren(datum);
				}
			}
		}

		protected virtual void UpdateNestedItem(TData data, float count, int depth)
		{
			UpdateVisibleItem(data, count);
		}

		protected void RecycleChildren(TData data)
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
