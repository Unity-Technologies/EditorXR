#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace ListView
{
	abstract class ListViewItemNestedData<ChildType, IndexType> : ListViewItemData<IndexType>
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

		public event Action<ListViewItemNestedData<ChildType, IndexType>, List<ChildType>> childrenChanging;
	}
}
#endif