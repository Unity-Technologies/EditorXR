using System;
using System.Collections.Generic;

namespace ListView
{
	public class ListViewItemNestedData<ChildType> : ListViewItemData
	{
		public List<ChildType> children
		{
			get { return m_Children; }
			set
			{
				if (childrenChanging != null)
					childrenChanging(this, value);

				m_Children = value;
			}
		}
		protected List<ChildType> m_Children;

		public event Action<ListViewItemNestedData<ChildType>, List<ChildType>> childrenChanging;
	}
}