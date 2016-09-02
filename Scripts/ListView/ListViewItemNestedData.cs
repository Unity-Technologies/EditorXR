namespace ListView
{
	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public bool expanded { get; set; }
		public ChildType[] children { get; protected set; }
	}
}