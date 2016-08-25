using UnityEngine;
using System.Collections.Generic;

namespace ListView
{
	public class ListViewController : ListViewController<ListViewItemInspectorData, ListViewItem>
	{
	}

	public abstract class ListViewControllerBase : MonoBehaviour
	{
		public float scrollOffset
		{
			get { return m_ScrollOffset; }
			set { m_ScrollOffset = value; }
		}
		[Tooltip("Distance (in meters) we have scrolled from initial position")]
		[SerializeField]
		protected float m_ScrollOffset;

		[Tooltip("Padding (in meters) between items")]
		[SerializeField]
		protected float m_Padding = 0.01f;

		[Tooltip("How quickly scroll momentum fade")]
		[SerializeField]
		public float m_ScrollDamping = 5f;

		[Tooltip("Maximum velocity for scroll momentum")]
		[SerializeField]
		public float m_MaxMomentum = 200f;

		[Tooltip("Item temlate prefabs (at least one is required)")]
		[SerializeField]
		protected GameObject[] m_Templates;
		
		protected int m_DataOffset;
		protected int m_NumRows;
		protected Vector3 m_StartPosition;
		protected Vector3 m_ItemSize;
		protected readonly Dictionary<string, ListViewItemTemplate> m_TemplateDictionary = new Dictionary<string, ListViewItemTemplate>();
		protected bool m_Scrolling;
		protected float m_ScrollReturn = float.MaxValue;
		protected float m_ScrollDelta;
		protected float m_LastScrollOffset;

		protected abstract int dataLength { get; }

		public Vector3 itemSize { get { return m_ItemSize; } }
		public Bounds bounds { protected get; set; }

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
			if (m_Templates.Length < 1)
			{
				Debug.LogError("No templates!");
			}
			foreach (var template in m_Templates)
			{
				if (m_TemplateDictionary.ContainsKey(template.name))
					Debug.LogError("Two templates cannot have the same name");
				m_TemplateDictionary[template.name] = new ListViewItemTemplate(template);
			}
		}

		protected virtual void ViewUpdate()
		{
			ComputeConditions();
			UpdateItems();
		}

		public void PreCompute() {
			ComputeConditions();
		}
		protected virtual void ComputeConditions()
		{
			if (m_Templates.Length > 0) {
				// Use first template to get item size
				m_ItemSize = GetObjectSize(m_Templates[0]);
			}

			m_NumRows = Mathf.CeilToInt(bounds.size.z / m_ItemSize.z);

			m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

			m_DataOffset = (int)(m_ScrollOffset / itemSize.z);
			if (m_ScrollOffset < 0)
				m_DataOffset--;

			if (m_Scrolling) {
				// Compute current velocity
				m_ScrollDelta = (m_ScrollOffset - m_LastScrollOffset) / Time.unscaledDeltaTime;
				m_LastScrollOffset = m_ScrollOffset;
				
				// Clamp velocity to MaxMomentum
				if (m_ScrollDelta > m_MaxMomentum)
					m_ScrollDelta = m_MaxMomentum;
				if (m_ScrollDelta < -m_MaxMomentum)
					m_ScrollDelta = -m_MaxMomentum;
			} else {
				//Apply scrolling momentum
				m_ScrollOffset += m_ScrollDelta * Time.unscaledDeltaTime;
				if(m_ScrollReturn < float.MaxValue || m_ScrollOffset > 0)
					OnEndScrolling();
				if (m_ScrollDelta > 0) {
					m_ScrollDelta -= m_ScrollDamping * Time.unscaledDeltaTime;
					if (m_ScrollDelta < 0) {
						m_ScrollDelta = 0;
						OnEndScrolling();
					}
				} else if (m_ScrollDelta < 0) {
					m_ScrollDelta += m_ScrollDamping * Time.unscaledDeltaTime;
					if (m_ScrollDelta > 0) {
						m_ScrollDelta = 0;
						OnEndScrolling();
					}
				}
			}

			m_ScrollReturn = float.MaxValue;

			// Snap back if list scrolled too far
			if (dataLength > 0 && -m_DataOffset >= dataLength)
				m_ScrollReturn = (1 - dataLength) * itemSize.z;
		}

		protected abstract void UpdateItems();

		public virtual void ScrollNext()
		{
			m_ScrollOffset += m_ItemSize.z;
		}

		public virtual void ScrollPrev()
		{
			m_ScrollOffset -= m_ItemSize.z;
		}

		public virtual void ScrollTo(int index)
		{
			m_ScrollOffset = index * itemSize.z;
		}

		protected virtual void UpdateItem(Transform t, int offset)
		{
			t.position = m_StartPosition + (offset * m_ItemSize.z + m_ScrollOffset) * Vector3.right;
		}

		protected virtual Vector3 GetObjectSize(GameObject g)
		{
			Vector3 itemSize = Vector3.one;
			//TODO: Better method for finding object size
			Renderer rend = g.GetComponentInChildren<Renderer>();
			if (rend)
			{
				itemSize.x = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).x * 2 + m_Padding;
				itemSize.y = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).y * 2 + m_Padding;
				itemSize.z = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).z * 2 + m_Padding;
			}
			return itemSize;
		}

		protected virtual void RecycleItem(string template, MonoBehaviour item)
		{
			if (item == null || template == null)
				return;
			m_TemplateDictionary[template].pool.Add(item);
			item.gameObject.SetActive(false);
		}

		public virtual void OnBeginScrolling() {
			m_Scrolling = true;
		}

		public virtual void OnEndScrolling() {
			m_Scrolling = false;
			if (m_ScrollOffset > 0) {
				m_ScrollOffset = 0;
				m_ScrollDelta = 0;
			}
			if (m_ScrollReturn < float.MaxValue) {
				m_ScrollOffset = m_ScrollReturn;
				m_ScrollReturn = float.MaxValue;
				m_ScrollDelta = 0;
			}
		}
	}

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
				} else if (i + m_DataOffset > m_NumRows - 1)
				{
					CleanUpEnd(m_Data[i]);
				} else
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
			{
				data.item = GetItem(data);
			}
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
			} else
			{
				item = Instantiate(m_TemplateDictionary[data.template].prefab).GetComponent<ItemType>();
				item.transform.parent = transform;
				item.Setup(data);
			}
			return item;
		}
	}
}