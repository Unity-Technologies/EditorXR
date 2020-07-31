using System;
using Unity.EditorXR.Interfaces;
using Unity.ListViewFramework;

namespace Unity.EditorXR
{
    class EditorXRListViewItem<TData, TIndex> : ListViewItem<TData, TIndex> where TData : IListViewItemData<TIndex>
    {
        public Action<Node> click { get; set; }
        public Action<Node> hoverStart { get; set; }
        public Action<Node> hoverEnd { get; set; }
        public Action<Node> dragStart { get; set; }
        public Action<Node> dragging { get; set; }
        public Action<Node> dragEnd { get; set; }
    }
}
