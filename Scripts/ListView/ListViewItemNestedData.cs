
using System;
using System.Collections.Generic;

namespace ListView
{
    abstract class ListViewItemNestedData<TChild, TIndex> : ListViewItemData<TIndex>
    {
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

        protected List<TChild> m_Children;

        public event Action<ListViewItemNestedData<TChild, TIndex>, List<TChild>> childrenChanging;
    }
}

