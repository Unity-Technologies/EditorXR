using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Tools;

namespace ListView
{
	public abstract class ListViewController<DataType, ItemType> : ListViewControllerBase, IInstantiateUI
		where DataType : ListViewItemData
		where ItemType : ListViewItem<DataType>
	{
		public virtual List<DataType> data
		{
			get { return m_Data; }
			set
			{
				if (m_Data != null)
				{
					foreach (var kvp in m_ListItems) // Clear out visuals for old data
					{
						RecycleItem(kvp.Key.template, kvp.Value);
					}

					m_ListItems.Clear();
				}

				m_Data = value;
				scrollOffset = 0;
			}
		}
		protected List<DataType> m_Data;

		protected readonly Dictionary<DataType, ItemType> m_ListItems = new Dictionary<DataType, ItemType>();

		protected override int dataLength { get { return m_Data.Count; } }

		public InstantiateUIDelegate instantiateUI { get; set; }

		protected override void UpdateItems()
		{
			for (int i = 0; i < m_Data.Count; i++)
			{
				var datum = m_Data[i];
				if (i + m_DataOffset < -1 || i + m_DataOffset > m_NumRows - 1)
					Recycle(datum);
				else
					UpdateVisibleItem(datum, i);
			}
		}

		protected virtual void Recycle(DataType data)
		{
			ItemType item;
			if (m_ListItems.TryGetValue(data, out item))
			{
				RecycleItem(data.template, item);
				m_ListItems.Remove(data);
			}
		}

		protected virtual void UpdateVisibleItem(DataType data, int offset)
		{
			ItemType item;
			if (!m_ListItems.TryGetValue(data, out item))
				m_ListItems[data] = GetItem(data);

			UpdateItemTransform(item.transform, offset);
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
				{
					item = instantiateUI(m_TemplateDictionary[data.template].prefab, transform, false).GetComponent<ItemType>();
				}
				else
				{
					item = Instantiate(m_TemplateDictionary[data.template].prefab).GetComponent<ItemType>();
					item.transform.SetParent(transform, false);
				}

				item.Setup(data);
			}

			m_ListItems[data] = item;

			return item;
		}
	}
}