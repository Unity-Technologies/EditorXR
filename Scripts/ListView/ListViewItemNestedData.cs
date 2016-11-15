namespace ListView
{
	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public ChildType[] children { get; protected set; }

		public bool defaultToExpanded { get { return m_DefaultToExpanded; } }
		readonly bool m_DefaultToExpanded;

		protected ListViewItemNestedData(bool defaultToExpanded = false)
		{
			m_DefaultToExpanded = defaultToExpanded;
		} 
	}
}