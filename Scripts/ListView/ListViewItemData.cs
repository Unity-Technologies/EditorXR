#if UNITY_EDITOR
namespace ListView
{
	public abstract class ListViewItemData<TIndex>
	{
		public abstract TIndex index { get; }
		public string template { get; protected set; }
	}
}
#endif
