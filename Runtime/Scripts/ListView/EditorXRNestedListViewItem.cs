using System;
using Unity.ListViewFramework;

namespace Unity.EditorXR
{
    abstract class EditorXRNestedListViewItem<TData, TIndex> : EditorXRListViewItem<TData, TIndex>, INestedListViewItem<TData, TIndex>
        where TData : INestedListViewItemData<TData, TIndex>
    {
        public event Action<TData> toggleExpanded;

        public void ToggleExpanded()
        {
            if (toggleExpanded != null)
                toggleExpanded(data);
        }
    }
}
