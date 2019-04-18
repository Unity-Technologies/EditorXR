namespace Unity.Labs.ListView
{
    public abstract class ListViewItemData<TIndex> : IListViewItemData<TIndex>
    {
        public TIndex index { get; protected set; }
        public string template { get; protected set; }
    }
}
