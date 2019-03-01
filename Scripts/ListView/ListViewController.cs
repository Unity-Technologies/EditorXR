using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;

namespace ListView
{
    public abstract class ListViewController<TData, TItem, TIndex> : ListViewControllerBase, IInstantiateUI, IConnectInterfaces, IControlHaptics, IRayToNode
        where TData : ListViewItemData<TIndex>
        where TItem : ListViewItem<TData, TIndex>
    {
#pragma warning disable 649
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

        protected List<TData> m_Data;

        protected readonly Dictionary<string, ListViewItemTemplate<TItem>> m_TemplateDictionary = new Dictionary<string, ListViewItemTemplate<TItem>>();
        protected readonly Dictionary<TIndex, TItem> m_ListItems = new Dictionary<TIndex, TItem>();
        protected readonly Dictionary<TIndex, Transform> m_GrabbedRows = new Dictionary<TIndex, Transform>();

        public virtual List<TData> data
        {
            get { return m_Data; }
            set
            {
                if (m_Data != null)
                {
                    foreach (var item in m_ListItems.Values) // Clear out visuals for old data
                    {
                        RecycleItem(item.data.template, item);
                    }

                    m_ListItems.Clear();
                }

                m_Data = value;
                scrollOffset = 0;
            }
        }

        protected override float listHeight { get { return m_Data.Count * itemSize.z; } }

        protected override void Setup()
        {
            if (m_Templates.Length < 1)
            {
                Debug.LogError("No templates!");
            }

            foreach (var template in m_Templates)
            {
                if (m_TemplateDictionary.ContainsKey(template.name))
                    Debug.LogError("Two templates cannot have the same name");

                m_TemplateDictionary[template.name] = new ListViewItemTemplate<TItem>(template);
            }
        }

        protected override void UpdateItems()
        {
            var doneSettling = true;

            var offset = 0f;
            var order = 0;
            for (int i = 0; i < m_Data.Count; i++)
            {
                var datum = m_Data[i];
                if (offset + scrollOffset + itemSize.z < 0 || offset + scrollOffset > m_Size.z)
                    Recycle(datum.index);
                else
                    UpdateVisibleItem(datum, order++, i * itemSize.z + m_ScrollOffset, ref doneSettling);

                offset += itemSize.z;
            }

            if (m_Settling && doneSettling)
                EndSettling();
        }

        protected virtual void Recycle(TIndex index)
        {
            if (m_GrabbedRows.ContainsKey(index))
                return;

            TItem item;
            if (m_ListItems.TryGetValue(index, out item))
            {
                RecycleItem(item.data.template, item);
                m_ListItems.Remove(index);
            }
        }

        protected virtual void RecycleItem(string template, TItem item)
        {
            if (item == null || template == null)
                return;

            m_TemplateDictionary[template].pool.Add(item);
            item.gameObject.SetActive(false);
        }

        protected virtual void UpdateVisibleItem(TData data, int order, float offset, ref bool doneSettling)
        {
            TItem item;
            var index = data.index;
            if (!m_ListItems.TryGetValue(index, out item))
            {
                item = GetItem(data);
                m_ListItems[index] = item;
            }

            UpdateItem(item.transform, order, offset, ref doneSettling);
        }

        protected virtual void SetRowGrabbed(TIndex index, Transform rayOrigin, bool grabbed)
        {
            if (grabbed)
                m_GrabbedRows[index] = rayOrigin;
            else
                m_GrabbedRows.Remove(index);
        }

        protected virtual TItem GetGrabbedRow(Transform rayOrigin)
        {
            foreach (var row in m_GrabbedRows)
            {
                if (row.Value == rayOrigin)
                    return GetListItem(row.Key);
            }
            return null;
        }

        protected TItem GetListItem(TIndex index)
        {
            TItem item;
            return m_ListItems.TryGetValue(index, out item) ? item : null;
        }

        protected virtual TItem GetItem(TData data)
        {
            if (data == null)
            {
                Debug.LogWarning("Tried to get item with null data");
                return null;
            }

            if (!m_TemplateDictionary.ContainsKey(data.template))
            {
                Debug.LogWarning("Cannot get item, template " + data.template + " doesn't exist");
                return null;
            }

            TItem item;
            if (m_TemplateDictionary[data.template].pool.Count > 0)
            {
                item = m_TemplateDictionary[data.template].pool[0];
                m_TemplateDictionary[data.template].pool.RemoveAt(0);

                item.gameObject.SetActive(true);
                item.Setup(data);
            }
            else
            {
                item = this.InstantiateUI(m_TemplateDictionary[data.template].prefab, transform, false).GetComponent<TItem>();
                this.ConnectInterfaces(item);
                item.Setup(data);

                // Hookup input events for new items.
                item.hoverStart += OnItemHoverStart;
                item.hoverEnd += OnItemHoverEnd;
                item.dragStart += OnItemDragStart;
                item.dragging += OnItemDragging;
                item.dragEnd += OnItemDragEnd;
                item.click += OnItemClicked;
            }

            m_ListItems[data.index] = item;

            item.startSettling = StartSettling;
            item.endSettling = EndSettling;
            item.getListItem = GetListItem;

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
