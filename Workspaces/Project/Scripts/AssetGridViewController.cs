#if !UNITY_EDITOR
#pragma warning disable 414, 649
#endif

using ListView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>, IConnectInterfaces
{
	private const float kPositionFollow = 0.4f;

	private Transform m_GrabbedObject;

	private int m_NumPerRow;

	public float scaleFactor
	{
		get
		{
			return m_ScaleFactor;
		}
		set
		{
			m_LastHiddenItemOffset = Mathf.Infinity; // Allow any change in scale to change visibility states
			m_ScaleFactor = value;
		}
	}

	[SerializeField]
	private float m_ScaleFactor = 0.1f;

	[SerializeField]
	private string[] m_IconTypes;

	[SerializeField]
	private GameObject[] m_Icons;

	float m_LastHiddenItemOffset;

	readonly Dictionary<string, GameObject> m_IconDictionary = new Dictionary<string, GameObject>();

	public ConnectInterfacesDelegate connectInterfaces { get; set; }

	public Func<string, bool> testFilter;

	protected override int dataLength { get { return Mathf.CeilToInt((float) base.dataLength / m_NumPerRow); } }

	public override List<AssetData> data
	{
		set
		{
			base.data = value;

			m_LastHiddenItemOffset = Mathf.Infinity;
		}
	}

#if UNITY_EDITOR
	protected override void Setup()
	{
		base.Setup();

		for (int i = 0; i < m_IconTypes.Length; i++)
		{
			if (!string.IsNullOrEmpty(m_IconTypes[i]) && m_Icons[i] != null)
				m_IconDictionary[m_IconTypes[i]] = m_Icons[i];
		}
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		var itemSize = m_ItemSize.Value;
		m_NumPerRow = (int) (bounds.size.x / itemSize.x);
		if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
			return;

		m_NumRows = (int) (bounds.size.z / itemSize.z);

		m_StartPosition = bounds.extents.z * Vector3.forward + (bounds.extents.x - itemSize.x * 0.5f) * Vector3.left;

		m_DataOffset = (int) (m_ScrollOffset / itemSize.z);
		if (m_ScrollOffset < 0)
			m_DataOffset --;


		// Snap back if list scrolled too far
		m_ScrollReturn = float.MaxValue;
		if (-m_DataOffset >= dataLength)
			m_ScrollReturn = (1 - dataLength) * itemSize.z + m_ScaleFactor;
	}

	protected override Vector3 GetObjectSize(GameObject g)
	{
		return g.GetComponent<BoxCollider>().size * m_ScaleFactor + Vector3.one * m_Padding;
	}

	protected override void UpdateItems()
	{
		var count = 0;
		foreach (var data in m_Data)
		{
			if (m_NumPerRow == 0) // If the list is too narrow, display nothing
			{
				RecycleGridItem(data);
				continue;
			}

			if (!testFilter(data.type)) // If this item doesn't match the filter, move on to the next item; do not count
			{
				RecycleGridItem(data);
				continue;
			}

			if (count / m_NumPerRow + m_DataOffset < 0 || count / m_NumPerRow + m_DataOffset > m_NumRows - 1)
				RecycleGridItem(data);
			else
				UpdateVisibleItem(data, count);

			count++;
		}
	}

	protected void RecycleGridItem(AssetData data)
	{
		AssetGridItem item;
		if (!m_ListItems.TryGetValue(data, out item))
			return;

		m_LastHiddenItemOffset = scrollOffset;

		m_ListItems.Remove(data);

		item.SetVisibility(false, gridItem =>
		{
			item.gameObject.SetActive(false);
			m_TemplateDictionary[data.template].pool.Add(item);
		});
	}

	protected override void UpdateVisibleItem(AssetData data, int offset)
	{
		AssetGridItem item;
		if (!m_ListItems.TryGetValue(data, out item))
			item = GetItem(data);

		if(item)
			UpdateGridItem(item, offset);
	}

	public override void OnScrollEnded()
	{
		m_Scrolling = false;
		if (m_ScrollOffset > m_ScaleFactor)
		{
			m_ScrollOffset = m_ScaleFactor;
			m_ScrollDelta = 0;
		}
		if (m_ScrollReturn < float.MaxValue)
		{
			m_ScrollOffset = m_ScrollReturn;
			m_ScrollReturn = float.MaxValue;
			m_ScrollDelta = 0;
		}
	}

	private void UpdateGridItem(AssetGridItem item, int offset)
	{
		item.UpdateTransforms(m_ScaleFactor);

		var itemSize = m_ItemSize.Value;
		var t = item.transform;
		var zOffset = itemSize.z * (offset / m_NumPerRow) + m_ScrollOffset;
		var xOffset = itemSize.x * (offset % m_NumPerRow);
		t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, kPositionFollow);
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData data)
	{
		const float kJitterMargin = 0.125f;
		if (Mathf.Abs(scrollOffset - m_LastHiddenItemOffset) < itemSize.z * kJitterMargin) // Avoid jitter while scrolling rows in and out of view
			return null;

		// If this AssetData hasn't fetched its asset yet, do so now
		if (data.asset == null)
		{
			data.asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid));
			data.preview = data.asset as GameObject;
		}

		var item = base.GetItem(data);

		item.transform.localPosition = m_StartPosition;
		connectInterfaces(item);

		item.scaleFactor = m_ScaleFactor;
		item.SetVisibility(true);

		switch (data.type)
		{
			case "Material":
				var material = data.asset as Material;
				if (material)
					item.material = material;
				else
					LoadFallbackTexture(item, data);
				break;
			case "Texture2D":
				goto case "Texture";
			case "Texture":
				var texture = data.asset as Texture;
				if (texture)
					item.texture = texture;
				else
					LoadFallbackTexture(item, data);
				break;
			default:
				GameObject icon;
				if (m_IconDictionary.TryGetValue(data.type, out icon))
					item.icon = icon;
				else
					LoadFallbackTexture(item, data);
				break;
		}
		return item;
	}

	static void LoadFallbackTexture(AssetGridItem item, AssetData data)
	{
		item.fallbackTexture = null;
		item.StartCoroutine(U.Object.GetAssetPreview(
			AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid)),
			texture => item.fallbackTexture = texture));
	}
#endif
}