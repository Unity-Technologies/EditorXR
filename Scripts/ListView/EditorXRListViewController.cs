using Unity.Labs.ListView;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    public abstract class EditorXRListViewController<TData, TItem, TIndex> : ListViewController<TData, TItem, TIndex>, IInstantiateUI, IConnectInterfaces, IControlHaptics, IRayToNode
        where TData : IListViewItemData<TIndex>
        where TItem : EditorXRListViewItem<TData, TIndex>
    {
#pragma warning disable 649
        [SerializeField]
        HapticPulse m_ScrollPulse;

        [Header("Unassigned haptic pulses will not be performed")]
        [SerializeField]
        HapticPulse m_ItemClickPulse;

        [SerializeField]
        HapticPulse m_ItemHoverStartPulse;

        [SerializeField]
        HapticPulse m_ItemHoverEndPulse;

        [SerializeField]
        HapticPulse m_ItemDragStartPulse;

        [SerializeField]
        HapticPulse m_ItemDraggingPulse;

        [SerializeField]
        HapticPulse m_ItemDragEndPulse;
#pragma warning restore 649

        protected override void Recycle(TIndex index)
        {
            if (m_GrabbedRows.ContainsKey(index))
                return;

            base.Recycle(index);
        }

        protected override void UpdateView()
        {
            base.UpdateView();

            if (m_Scrolling)
                this.Pulse(Node.None, m_ScrollPulse);
        }

        protected override TItem InstantiateItem(TData data)
        {
            var item = this.InstantiateUI(m_TemplateDictionary[data.template].prefab, transform, false).GetComponent<TItem>();
            this.ConnectInterfaces(item);

            // Hookup input events for new items.
            item.hoverStart += OnItemHoverStart;
            item.hoverEnd += OnItemHoverEnd;
            item.dragStart += OnItemDragStart;
            item.dragging += OnItemDragging;
            item.dragEnd += OnItemDragEnd;
            item.click += OnItemClicked;
            return item;
        }

        public void OnItemHoverStart(Node node)
        {
            if (m_ItemHoverStartPulse)
                this.Pulse(node, m_ItemHoverStartPulse);
        }

        public void OnItemHoverEnd(Node node)
        {
            if (m_ItemHoverEndPulse)
                this.Pulse(node, m_ItemHoverEndPulse);
        }

        public void OnItemDragStart(Node node)
        {
            if (m_ItemDragStartPulse)
                this.Pulse(node, m_ItemDragStartPulse);
        }

        public void OnItemDragging(Node node)
        {
            if (m_ItemDraggingPulse)
                this.Pulse(node, m_ItemDraggingPulse);
        }

        public void OnItemDragEnd(Node node)
        {
            if (m_ItemDragEndPulse)
                this.Pulse(node, m_ItemDragEndPulse);
        }

        public void OnItemClicked(Node node)
        {
            if (m_ItemClickPulse)
                this.Pulse(node, m_ItemClickPulse);
        }
    }
}
