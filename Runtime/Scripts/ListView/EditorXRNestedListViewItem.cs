using System;
using Unity.Labs.ListView;

namespace UnityEditor.Experimental.EditorVR
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
