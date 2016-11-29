using System;

namespace ListView
{
	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public ChildType[] children
		{
			get { return m_Children; }
			set
			{
				if (childrenChanging != null)
					childrenChanging(this, value);

				m_Children = value;
			}
		}
		protected ChildType[] m_Children;

		public event Action<ListViewItemNestedData<ChildType>, ChildType[]> childrenChanging;
	}
}