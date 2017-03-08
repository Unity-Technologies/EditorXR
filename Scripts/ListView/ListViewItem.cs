#if UNITY_EDITOR
using System;
using UnityEngine;

namespace ListView
{
	public class ListViewItem<DataType, IndexType> : MonoBehaviour where DataType : ListViewItemData<IndexType>
	{
		protected bool m_Settling;

		public DataType data { get; set; }
		public Action<Action> startSettling { protected get; set; }
		public Action endSettling { protected get; set; }

		public virtual void Setup(DataType data)
		{
			this.data = data;
		}

		public virtual void OnStartSettling()
		{
			m_Settling = true;
		}

		public virtual void OnEndSettling()
		{
			m_Settling = false;
		}
	}
}
#endif
