using System;
using Unity.Labs.ListView;

namespace Unity.Labs.EditorXR.Workspaces
{
    class NestedDraggableListItem<TData, TIndex> : DraggableListItem<TData, TIndex>, INestedListViewItem<TData, TIndex>
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
