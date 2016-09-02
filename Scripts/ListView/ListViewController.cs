using UnityEngine;

namespace ListView
{
	public abstract class ListViewController<DataType, ItemType> : ListViewControllerBase
		where DataType : ListViewItemData
		where ItemType : ListViewItem<DataType>
	{
		[SerializeField]
		protected DataType[] m_Data;

		protected override int dataLength { get { return m_Data.Length; } }

		protected override void UpdateItems()
		{
			for (int i = 0; i < m_Data.Length; i++)
			{
				if (i + m_DataOffset < -1)
				{
					CleanUpBeginning(m_Data[i]);
				}
				else if (i + m_DataOffset > m_NumRows - 1)
				{
					CleanUpEnd(m_Data[i]);
				}
				else
				{
					UpdateVisibleItem(m_Data[i], i);
				}
			}
		}

		protected virtual void CleanUpBeginning(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void CleanUpEnd(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void UpdateVisibleItem(DataType data, int offset)
		{
			if (data.item == null)
				data.item = GetItem(data);
			UpdateItem(data.item.transform, offset);
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
			ItemType item = null;
			if (m_TemplateDictionary[data.template].pool.Count > 0)
			{
				item = (ItemType) m_TemplateDictionary[data.template].pool[0];
				m_TemplateDictionary[data.template].pool.RemoveAt(0);

				item.gameObject.SetActive(true);
				item.Setup(data);
			}
			else
			{
				item = Instantiate(m_TemplateDictionary[data.template].prefab).GetComponent<ItemType>();
				item.transform.SetParent(transform, false);
				item.Setup(data);
			}
			return item;
		}
	}
}