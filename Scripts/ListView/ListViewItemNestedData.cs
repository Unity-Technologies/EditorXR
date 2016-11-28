namespace ListView
{
	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public ChildType[] children { get; protected set; }
	}
}