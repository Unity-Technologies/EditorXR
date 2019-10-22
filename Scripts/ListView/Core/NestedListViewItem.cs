using System;

namespace Unity.Labs.ListView
{
    public abstract class NestedListViewItem<TData, TIndex> : ListViewItem<TData, TIndex>,
        INestedListViewItem<TData, TIndex> where TData : IListViewItemData<TIndex>
    {
        public event Action<TData> toggleExpanded;

        public void ToggleExpanded()
        {
            if (toggleExpanded != null)
                toggleExpanded(data);
        }
    }
}
