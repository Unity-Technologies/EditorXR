using System;
using UnityEngine;
using UnityEngine.VR.Tools;

namespace ListView
{
	public abstract class ListViewController<DataType, ItemType> : ListViewControllerBase, IInstantiateUI
		where DataType : ListViewItemData
		where ItemType : ListViewItem<DataType>
	{
		public virtual DataType[] data
		{
			get { return m_Data; }
			set
			{
				if (m_Data != null)
				{
					foreach (var data in m_Data) // Clear out visuals for old data
					{
						RecycleBeginning(data);
					}
				}
				m_Data = value;
				scrollOffset = 0;
			}
		}
		[SerializeField]
		protected DataType[] m_Data;

		protected override int dataLength { get { return m_Data.Length; } }

		public Func<GameObject, GameObject> instantiateUI { get; set; }

		protected override void UpdateItems()
		{
			for (int i = 0; i < m_Data.Length; i++)
			{
				var datum = m_Data[i];
				if (i + m_DataOffset < -1)
					RecycleBeginning(datum);
				else if (i + m_DataOffset > m_NumRows - 1)
					RecycleEnd(datum);
				else
					UpdateVisibleItem(datum, i);
			}
		}

		protected virtual void RecycleBeginning(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void RecycleEnd(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void UpdateVisibleItem(DataType data, int offset)
		{
			if (data.item == null)
				data.item = GetItem(data);

			UpdateItemTransform(data.item.transform, offset);
		}

		protected virtual ItemType GetItem(DataType data)
		{
			if (data == null)
			{
				Debug.LogWarning("Tried to get item with null m_Data");
				return null;
			}
			if (!m_TemplateDictionary.ContainsKey(data.template))
			{
				Debug.LogWarning("Cannot get item, template " + data.template + " doesn't exist");
				return null;
			}
			ItemType item;
			if (m_TemplateDictionary[data.template].pool.Count > 0)
			{
				item = (ItemType) m_TemplateDictionary[data.template].pool[0];
				m_TemplateDictionary[data.template].pool.RemoveAt(0);

				item.gameObject.SetActive(true);
				item.Setup(data);
			}
			else
			{
				if (instantiateUI != null)
					item = instantiateUI(m_TemplateDictionary[data.template].prefab).GetComponent<ItemType>();
				else
					item = Instantiate(m_TemplateDictionary[data.template].prefab).GetComponent<ItemType>();
				item.transform.SetParent(transform, false);
				item.Setup(data);
			}
			return item;
		}
	}
}