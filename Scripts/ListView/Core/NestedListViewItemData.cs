using System;
using System.Collections.Generic;

namespace Unity.Labs.ListView
{
    public abstract class NestedListViewItemData<TChild, TIndex> : ListViewItemData<TIndex>, INestedListViewItemData<TChild, TIndex>
    {
        protected List<TChild> m_Children;

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

        public event Action<INestedListViewItemData<TChild, TIndex>, List<TChild>> childrenChanging;
    }
}
