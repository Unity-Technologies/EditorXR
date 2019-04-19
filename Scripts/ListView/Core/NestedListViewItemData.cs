using System;
using System.Collections.Generic;

namespace Unity.Labs.ListView
{
    public abstract class NestedListViewItemData<TChild, TIndex> : INestedListViewItemData<TChild, TIndex>
    {
        protected List<TChild> m_Children;

        public abstract TIndex index { get; }
        public abstract string template { get; }

        public event Action<INestedListViewItemData<TChild, TIndex>, List<TChild>> childrenChanging;

        public List<TChild> children
        {
            get { return m_Children; }
            set
            {
                if (childrenChanging != null)
                    childrenChanging(this, value);

                m_Children = value;
            }
        }
    }
}
