namespace Unity.Labs.ListView
{
    public interface IListViewItemData<TIndex>
    {
        TIndex index { get; }
        string template { get; }
    }
}
