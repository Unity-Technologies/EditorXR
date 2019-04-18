using System;
using System.Collections.Generic;

#if LISTVIEW_TESTS
using System.Diagnostics;
#endif

namespace Unity.Labs.ListView
{
    public abstract class NestedListViewController<TData, TItem, TIndex> : ListViewController<TData, TItem, TIndex>
        where TData : INestedListViewItemData<TData, TIndex>
        where TItem : INestedListViewItem<TData, TIndex>
    {
        protected struct UpdateData
        {
            public List<TData> data;
            public int depth;

            public int index;
        }

        protected override float listHeight
        {
            get { return m_ExpandedDataLength; }
        }

        protected float m_ExpandedDataLength;

        protected readonly Dictionary<TIndex, bool> m_ExpandStates = new Dictionary<TIndex, bool>();

        public override List<TData> data
        {
            get { return base.data; }
            set
            {
                m_Data = value;

                // Update visible rows if data has changed, recycle if data is missing
                foreach (var row in new Dictionary<TIndex, TItem>(m_ListItems))
                {
                    var index = row.Key;
                    var newData = GetRowRecursive(m_Data, index);
                    if (newData != null)
                        row.Value.Setup(newData, false);
                    else
                        Recycle(index);
                }
            }
        }

        // Local method use only -- created here to reduce garbage collection
        Action<TData> m_ToggleExpanded;
        protected readonly Stack<UpdateData> m_UpdateStack = new Stack<UpdateData>();

        protected override void Awake()
        {
            base.Awake();
            m_ToggleExpanded = ToggleExpanded;
        }

        static TData GetRowRecursive(List<TData> data, TIndex index)
        {
            foreach (var datum in data)
            {
                if (datum.index.Equals(index))
                    return datum;

                var children = datum.children;
                if (children != null)
                {
                    var result = GetRowRecursive(children, index);
                    if (result != null)
                        return result;
                }
            }

            return default(TData);
        }

        protected void RecycleRecursively(TData datum)
        {
            Recycle(datum.index);

            var children = datum.children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    RecycleRecursively(child);
                }
            }
        }

        protected override void UpdateItems()
        {
            var doneSettling = true;
            var offset = 0f;
            var order = 0;

            UpdateNestedItems(ref order, ref offset, ref doneSettling);
            m_ExpandedDataLength = offset;

            if (m_Settling && doneSettling)
                EndSettling();
        }

        protected virtual void UpdateNestedItems(ref int order, ref float offset, ref bool doneSettling, int depth = 0)
        {
#if LISTVIEW_TESTS
            Debug.Assert(m_UpdateStack.Count == 0);
#endif

            // We assume that this stack is empty because of the while loop below;
            // It is possible for it to contain data if an exception is thrown inside the loop
            m_UpdateStack.Push(new UpdateData
            {
                data = m_Data,
                depth = depth
            });

            var itemWidth = m_ItemSize.z;
            var listWidth = m_Size.z;
            while (m_UpdateStack.Count > 0)
            {
                var stackData = m_UpdateStack.Pop();
                var nestedData = stackData.data;
                depth = stackData.depth;

                var i = stackData.index;
                for (; i < nestedData.Count; i++)
                {
                    var datum = nestedData[i];

                    var index = datum.index;
                    bool expanded;
                    if (!m_ExpandStates.TryGetValue(index, out expanded))
                        m_ExpandStates[index] = false;

                    var localOffset = offset + m_ScrollOffset;
                    if (localOffset + itemWidth < 0 || localOffset > listWidth)
                        Recycle(index);
                    else
                        UpdateNestedItem(datum, order++, localOffset, depth, ref doneSettling);

                    offset += itemWidth;

                    if (datum.children != null)
                    {
                        if (expanded)
                        {
                            m_UpdateStack.Push(new UpdateData
                            {
                                data = nestedData,
                                depth = depth,
                                index = i + 1
                            });

                            m_UpdateStack.Push(new UpdateData
                            {
                                data = datum.children,
                                depth = depth + 1
                            });
                            break;
                        }

                        RecycleChildren(datum);
                    }
                }
            }
        }

        protected virtual void UpdateNestedItem(TData datum, int order, float offset, int depth, ref bool doneSettling)
        {
            UpdateVisibleItem(datum, order, offset, ref doneSettling);
        }

        protected void RecycleChildren(TData datum)
        {
            var children = datum.children;
            foreach (var child in children)
            {
                Recycle(child.index);

                if (child.children != null)
                    RecycleChildren(child);
            }
        }

        protected bool GetExpanded(TIndex index)
        {
            bool expanded;
            m_ExpandStates.TryGetValue(index, out expanded);
            return expanded;
        }

        protected void SetExpanded(TIndex index, bool expanded)
        {
            m_ExpandStates[index] = expanded;
            StartSettling();
        }

        protected void ScrollToIndex(TData container, TIndex targetIndex, ref float scrollHeight)
        {
            var index = container.index;
            if (index.Equals(targetIndex))
            {
                if (-m_ScrollOffset > scrollHeight || -m_ScrollOffset + m_Size.z < scrollHeight)
                    m_ScrollOffset = -scrollHeight;

                return;
            }

            scrollHeight += m_ItemSize.z;

            if (GetExpanded(index))
            {
                var children = container.children;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        ScrollToIndex(child, targetIndex, ref scrollHeight);
                    }
                }
            }
        }

        protected override bool GetNewItem(TData datum, out TItem item)
        {
            var instantiated = base.GetNewItem(datum, out item);
            if (instantiated)
                item.toggleExpanded += m_ToggleExpanded;

            return instantiated;
        }

        protected virtual void ToggleExpanded(TData datum)
        {
            bool expanded;
            m_ExpandStates.TryGetValue(datum.index, out expanded);
            m_ExpandStates[datum.index] = !expanded;
            StartSettling();
        }
    }
}
