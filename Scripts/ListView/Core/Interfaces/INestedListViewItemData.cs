using System;
using System.Collections.Generic;

namespace Unity.Labs.ListView
{
    public interface INestedListViewItemData<TChild, TIndex> : IListViewItemData<TIndex>
    {
        List<TChild> children { get; }
        event Action<INestedListViewItemData<TChild, TIndex>, List<TChild>> childrenChanging;
    }
}
