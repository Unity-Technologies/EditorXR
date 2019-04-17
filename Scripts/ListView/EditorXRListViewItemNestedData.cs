using ListView;
using System;

namespace UnityEditor.Experimental.EditorVR
{
    abstract class EditorXRListViewItemNestedData<TChild, TIndex> : ListViewItemNestedData<TChild, TIndex>
    {
        public Action<Node> click { get; set; }
        public Action<Node> hoverStart { get; set; }
        public Action<Node> hoverEnd { get; set; }
        public Action<Node> dragStart { get; set; }
        public Action<Node> dragging { get; set; }
        public Action<Node> dragEnd { get; set; }
    }
}
