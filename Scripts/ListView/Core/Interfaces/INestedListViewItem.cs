using System;

namespace Unity.Labs.ListView
{
    public interface INestedListViewItem : IListViewItem
    {
        void ToggleExpanded();
    }

    public interface INestedListViewItem<TData, TIndex> : INestedListViewItem, IListViewItem<TData, TIndex>
        where TData : IListViewItemData<TIndex>
    {
        event Action<TData> toggleExpanded;
    }
}
