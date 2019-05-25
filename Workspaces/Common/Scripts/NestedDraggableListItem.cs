using System;
using UnityEngine;
using Unity.Labs.ListView;

namespace UnityEditor.Experimental.EditorVR.Workspaces
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
