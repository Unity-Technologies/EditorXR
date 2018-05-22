
namespace ListView
{
    public abstract class ListViewItemData<TIndex>
    {
        public virtual TIndex index { get; protected set; }
        public string template { get; protected set; }
    }
}

