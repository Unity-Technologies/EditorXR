using System.Collections.Generic;
using UnityEngine;

namespace ListView
{
	public abstract class ListViewControllerBase : MonoBehaviour
	{
		public float scrollOffset { get { return m_ScrollOffset; } set { m_ScrollOffset = value; } }

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

		public Vector3 itemSize
		{
			get
			{
				if (!m_ItemSize.HasValue && m_Templates.Length > 0)
					m_ItemSize = GetObjectSize(m_Templates[0]);

				return m_ItemSize ?? Vector3.zero;
			}
		}

		protected Vector3? m_ItemSize;

		protected int m_DataOffset;
		protected int m_NumRows;
		protected Vector3 m_StartPosition;

		protected readonly Dictionary<string, ListViewItemTemplate> m_TemplateDictionary = new Dictionary<string, ListViewItemTemplate>();

		protected bool m_Scrolling;
		protected float m_ScrollReturn = float.MaxValue;
		protected float m_ScrollDelta;
		protected float m_LastScrollOffset;

		protected abstract int dataLength { get; }

		public Bounds bounds { protected get; set; }

		void Start()
		{
			Setup();
		}

		void Update()
		{
			UpdateView();
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

		protected virtual void UpdateView()
		{
			ComputeConditions();
			UpdateItems();
		}

		protected virtual void ComputeConditions()
		{
			if (m_Templates.Length > 0) // Use first template to get item size
				m_ItemSize = GetObjectSize(m_Templates[0]);

			var itemSize = m_ItemSize.Value;
			m_NumRows = Mathf.CeilToInt(bounds.size.z / itemSize.z);

			m_StartPosition = (bounds.extents.z - itemSize.z * 0.5f) * Vector3.forward;

			m_DataOffset = (int) (m_ScrollOffset / itemSize.z);
			if (m_ScrollOffset < 0)
				m_DataOffset--;

			if (m_Scrolling)
			{
				// Compute current velocity, clamping value for better appearance when applying scrolling momentum
				const float kScrollDeltaClamp = 0.6f;
				m_ScrollDelta = Mathf.Clamp((m_ScrollOffset - m_LastScrollOffset) / Time.unscaledDeltaTime, -kScrollDeltaClamp, kScrollDeltaClamp);
				m_LastScrollOffset = m_ScrollOffset;

				// Clamp velocity to MaxMomentum
				if (m_ScrollDelta > m_MaxMomentum)
					m_ScrollDelta = m_MaxMomentum;
				if (m_ScrollDelta < -m_MaxMomentum)
					m_ScrollDelta = -m_MaxMomentum;
			}
			else
			{
				//Apply scrolling momentum
				m_ScrollOffset += m_ScrollDelta * Time.unscaledDeltaTime;
				const float kScrollMomentumShape = 2f;
				if (m_ScrollReturn < float.MaxValue || m_ScrollOffset > 0)
					OnScrollEnded();

				if (m_ScrollDelta > 0)
				{
					m_ScrollDelta -= Mathf.Pow(m_ScrollDamping, kScrollMomentumShape) * Time.unscaledDeltaTime;
					if (m_ScrollDelta < 0)
					{
						m_ScrollDelta = 0;
						OnScrollEnded();
					}
				}
				else if (m_ScrollDelta < 0)
				{
					m_ScrollDelta += Mathf.Pow(m_ScrollDamping, kScrollMomentumShape) * Time.unscaledDeltaTime;
					if (m_ScrollDelta > 0)
					{
						m_ScrollDelta = 0;
						OnScrollEnded();
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
			m_ScrollOffset += m_ItemSize.Value.z;
		}

		public virtual void ScrollPrev()
		{
			m_ScrollOffset -= m_ItemSize.Value.z;
		}

		public virtual void ScrollTo(int index)
		{
			m_ScrollOffset = index * itemSize.z;
		}

		protected virtual void UpdateItemTransform(Transform t, int offset)
		{
			t.localPosition = m_StartPosition + (offset * m_ItemSize.Value.z + m_ScrollOffset) * Vector3.back;
			t.localRotation = Quaternion.identity;
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

		public virtual void OnBeginScrolling()
		{
			m_Scrolling = true;
		}

		public virtual void OnScrollEnded()
		{
			m_Scrolling = false;

			if (m_ScrollOffset > 0)
			{
				m_ScrollOffset = 0;
				m_ScrollDelta = 0;
			}
			if (m_ScrollReturn < float.MaxValue)
			{
				m_ScrollOffset = m_ScrollReturn;
				m_ScrollReturn = float.MaxValue;
				m_ScrollDelta = 0;
			}
		}

		protected void SetMaterialClip(Material material, Matrix4x4 parentMatrix)
		{
			material.SetMatrix("_ParentMatrix", parentMatrix);
			material.SetVector("_ClipExtents", bounds.extents);
		}
	}
}