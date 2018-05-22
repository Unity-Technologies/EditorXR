
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;

namespace ListView
{
    public class ListViewItem<TData, TIndex> : MonoBehaviour where TData : ListViewItemData<TIndex>
    {
        public TData data { get; set; }
        public Action<Action> startSettling { protected get; set; }
        public Action endSettling { protected get; set; }
        public Func<TIndex, ListViewItem<TData, TIndex>> getListItem { protected get; set; }

        public Action<Node> click { get; set; }
        public Action<Node> hoverStart { get; set; }
        public Action<Node> hoverEnd { get; set; }
        public Action<Node> dragStart { get; set; }
        public Action<Node> dragging { get; set; }
        public Action<Node> dragEnd { get; set; }

        public virtual void Setup(TData data)
        {
            this.data = data;
        }
    }
}

