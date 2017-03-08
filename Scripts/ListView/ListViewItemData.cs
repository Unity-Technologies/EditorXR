#if UNITY_EDITOR
namespace ListView
{
	public abstract class ListViewItemData<IndexType>
	{
		public abstract IndexType index { get; }
		public string template { get; protected set; }
	}
}
#endif
