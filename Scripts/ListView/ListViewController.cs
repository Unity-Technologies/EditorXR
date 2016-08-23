using UnityEngine;
using System.Collections.Generic;

namespace ListView
{
	public class ListViewController : ListViewController<ListViewItemInspectorData, ListViewItem>
	{
	}

	public abstract class ListViewControllerBase : MonoBehaviour
	{
		//Public variables
		[Tooltip("Distance (in meters) we have scrolled from initial position")]
		public float scrollOffset;

		[Tooltip("Padding (in meters) between items")]
		public float padding = 0.01f;

		[Tooltip("Width (in meters) of visible region")]
		public float range = 1;

		[Tooltip("Item temlate prefabs (at least one is required)")]
		public GameObject[] templates;

		//Protected variables
		protected int m_DataOffset;
		protected int m_NumItems;
		protected Vector3 m_StartPosition;
		protected Vector3 m_ItemSize;
		protected readonly Dictionary<string, ListViewItemTemplate> m_Templates = new Dictionary<string, ListViewItemTemplate>();

		//Public properties
		public Vector3 itemSize
		{
			get { return m_ItemSize; }
		}

		void Start()
		{
			Setup();
		}

		void Update()
		{
			ViewUpdate();
		}

		protected virtual void Setup()
		{
			if (templates.Length < 1)
			{
				Debug.LogError("No templates!");
			}
			foreach (var template in templates)
			{
				if (m_Templates.ContainsKey(template.name))
					Debug.LogError("Two templates cannot have the same name");
				m_Templates[template.name] = new ListViewItemTemplate(template);
			}
		}

		protected virtual void ViewUpdate()
		{
			ComputeConditions();
			UpdateItems();
		}

		protected virtual void ComputeConditions()
		{
			if (templates.Length > 0)
			{
				//Use first template to get item size
				m_ItemSize = GetObjectSize(templates[0]);
			}
			//Resize range to nearest multiple of item width
			m_NumItems = Mathf.RoundToInt(range / m_ItemSize.x); //Number of cards that will fit
			range = m_NumItems * m_ItemSize.x;

			//Get initial conditions. This procedure is done every frame in case the collider bounds change at runtime
			m_StartPosition = transform.position + Vector3.left * range * 0.5f;

			m_DataOffset = (int) (scrollOffset / itemSize.x);
			if (scrollOffset < 0)
				m_DataOffset--;
		}

		protected abstract void UpdateItems();

		public virtual void ScrollNext()
		{
			scrollOffset += m_ItemSize.x;
		}

		public virtual void ScrollPrev()
		{
			scrollOffset -= m_ItemSize.x;
		}

		public virtual void ScrollTo(int index)
		{
			scrollOffset = index * itemSize.x;
		}

		protected virtual void Positioning(Transform t, int offset)
		{
			t.position = m_StartPosition + (offset * m_ItemSize.x + scrollOffset) * Vector3.right;
		}

		protected virtual Vector3 GetObjectSize(GameObject g)
		{
			Vector3 itemSize = Vector3.one;
			//TODO: Better method for finding object size
			Renderer rend = g.GetComponentInChildren<Renderer>();
			if (rend)
			{
				itemSize.x = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).x * 2 + padding;
				itemSize.y = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).y * 2 + padding;
				itemSize.z = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).z * 2 + padding;
			}
			return itemSize;
		}

		protected virtual void RecycleItem(string template, MonoBehaviour item)
		{
			if (item == null || template == null)
				return;
			m_Templates[template].pool.Add(item);
			item.gameObject.SetActive(false);
		}
	}

	public abstract class ListViewController<DataType, ItemType> : ListViewControllerBase
		where DataType : ListViewItemData
		where ItemType : ListViewItem<DataType>
	{
		[Tooltip("Source Data")]
		public DataType[] data;

		protected override void UpdateItems()
		{
			for (int i = 0; i < data.Length; i++)
			{
				if (i + m_DataOffset < 0)
				{
					ExtremeLeft(data[i]);
				} else if (i + m_DataOffset > m_NumItems)
				{
					ExtremeRight(data[i]);
				} else
				{
					ListMiddle(data[i], i);
				}
			}
		}

		protected virtual void ExtremeLeft(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void ExtremeRight(DataType data)
		{
			RecycleItem(data.template, data.item);
			data.item = null;
		}

		protected virtual void ListMiddle(DataType data, int offset)
		{
			if (data.item == null)
			{
				data.item = GetItem(data);
			}
			Positioning(data.item.transform, offset);
		}

		protected virtual ItemType GetItem(DataType data)
		{
			if (data == null)
			{
				Debug.LogWarning("Tried to get item with null data");
				return null;
			}
			if (!m_Templates.ContainsKey(data.template))
			{
				Debug.LogWarning("Cannot get item, template " + data.template + " doesn't exist");
				return null;
			}
			ItemType item = null;
			if (m_Templates[data.template].pool.Count > 0)
			{
				item = (ItemType) m_Templates[data.template].pool[0];
				m_Templates[data.template].pool.RemoveAt(0);

				item.gameObject.SetActive(true);
				item.Setup(data);
			} else
			{
				item = Instantiate(m_Templates[data.template].prefab).GetComponent<ItemType>();
				item.transform.parent = transform;
				item.Setup(data);
			}
			return item;
		}
	}
}