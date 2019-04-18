using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.ListView
{
    public abstract class ListViewController<TData, TItem, TIndex> : ListViewControllerBase
        where TData : IListViewItemData<TIndex>
        where TItem : IListViewItem<TData, TIndex>
    {
        protected List<TData> m_Data;
        IListViewItem<TData, TIndex> m_LastUpdatedItemItem;

        protected readonly Dictionary<string, ListViewItemTemplate<TItem>> m_TemplateDictionary = new Dictionary<string, ListViewItemTemplate<TItem>>();
        protected readonly Dictionary<TIndex, TItem> m_ListItems = new Dictionary<TIndex, TItem>();
        protected readonly Dictionary<TIndex, Transform> m_GrabbedRows = new Dictionary<TIndex, Transform>();

        // Local method use only -- created here to reduce garbage collection
        Action<Action> m_StartSettling;
        Action m_EndSettling;
        Func<TIndex, IListViewItem<TData, TIndex>> m_GetListItem;

        public virtual List<TData> data
        {
            get { return m_Data; }
            set
            {
                if (m_Data != null)
                {
                    foreach (var kvp in m_ListItems)
                    {
                        var item = kvp.Value;
                        RecycleItem(item.data.template, item);
                    }

                    m_ListItems.Clear();
                }

                m_Data = value;
                scrollOffset = 0;
            }
        }

        protected override float listHeight
        {
            get { return m_Data == null ? 0 : m_Data.Count * m_ItemSize.z; }
        }

        protected override void Awake()
        {
            base.Awake();

            m_StartSettling = StartSettling;
            m_EndSettling = EndSettling;
            m_GetListItem = index => GetListItem(index);
        }

        protected virtual void Start()
        {
            m_TemplateDictionary.Clear();
            if (m_Templates.Length < 1)
                Debug.LogError("No templates!");

            foreach (var template in m_Templates)
            {
                var templateName = template.name;
                if (m_TemplateDictionary.ContainsKey(templateName))
                    Debug.LogError("Two templates cannot have the same name");

                m_TemplateDictionary[templateName] = new ListViewItemTemplate<TItem>(template);
            }
        }

        protected override void UpdateItems()
        {
            var doneSettling = true;

            var offset = 0f;
            var order = 0;
            var itemWidth = m_ItemSize.z;
            var listWidth = m_Size.z;
            var count = m_Data.Count;
            for (var i = 0; i < count; i++)
            {
                var datum = m_Data[i];
                var localOffset = offset + scrollOffset;
                if (localOffset + itemWidth < 0 || localOffset > listWidth)
                    Recycle(datum.index);
                else
                    UpdateVisibleItem(datum, order++, localOffset, ref doneSettling);

                offset += itemWidth;
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

            m_TemplateDictionary[template].pool.Enqueue(item);
            item.SetActive(false);
        }

        protected virtual void UpdateVisibleItem(TData datum, int order, float offset, ref bool doneSettling)
        {
            TItem item;
            var index = datum.index;
            if (!m_ListItems.TryGetValue(index, out item))
                GetNewItem(datum, out item);

            m_LastUpdatedItemItem = item;
            UpdateItem(item, order, offset, ref doneSettling);
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

            return default(TItem);
        }

        protected TItem GetListItem(TIndex index)
        {
            TItem item;
            return m_ListItems.TryGetValue(index, out item) ? item : default(TItem);
        }

        /// <summary>
        /// Get a view item for a given datum, either from its template's pool, or by creating a new one
        /// </summary>
        /// <param name="datum">The datum for the desired view item</param>
        /// <param name="item">The view item</param>
        /// <returns>True if a new item was instantiated, false if the view item came from the item pool</returns>
        protected virtual bool GetNewItem(TData datum, out TItem item)
        {
            if (datum == null)
            {
                Debug.LogWarning("Tried to get item with null datum");
                item = default(TItem);
                return false;
            }

            var templateName = datum.template;
            ListViewItemTemplate<TItem> template;
            if (!m_TemplateDictionary.TryGetValue(templateName, out template))
            {
                Debug.LogWarning(string.Format("Cannot get item, template {0} doesn't exist", templateName));
                item = default(TItem);
                return false;
            }

            var pool = template.pool;
            var pooled = pool.Count > 0;
            if (pooled)
            {
                item = pool.Dequeue();
                item.SetActive(true);
                item.Setup(datum, false);
            }
            else
            {
                item = InstantiateItem(datum);
                item.Setup(datum, true);
                item.startSettling = m_StartSettling;
                item.endSettling = m_EndSettling;
                item.getListItem = m_GetListItem;
            }

            m_ListItems[datum.index] = item;

            if (m_LastUpdatedItemItem != null)
                item.localPosition = m_LastUpdatedItemItem.localPosition;

            return !pooled;
        }

        protected virtual TItem InstantiateItem(TData data)
        {
            return Instantiate(m_TemplateDictionary[data.template].prefab, transform, false).GetComponent<TItem>();
        }
    }
}
