using ListView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>, IConnectInterfaces
{
	private const float kTransitionDuration = 0.1f;
	private const float kPositionFollow = 0.4f;

	private Transform m_GrabbedObject;

	private int m_NumPerRow;

	public float scaleFactor { get { return m_ScaleFactor; } set { m_ScaleFactor = value; } }

	[SerializeField]
	private float m_ScaleFactor = 0.1f;

	[SerializeField]
	private string[] m_IconTypes;

	[SerializeField]
	private GameObject[] m_Icons;

	public ConnectInterfacesDelegate connectInterfaces { get; set; }

	public Func<string, bool> testFilter;

	protected override int dataLength { get { return Mathf.CeilToInt((float) base.dataLength / m_NumPerRow); } }
	private readonly Dictionary<string, GameObject> m_IconDictionary = new Dictionary<string, GameObject>();

	public override AssetData[] data
	{
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
		}
	}

	protected override void Setup()
	{
		base.Setup();

		m_Data = new AssetData[0]; // Start with empty list to avoid null references

		for (int i = 0; i < m_IconTypes.Length; i++)
		{
			if (!string.IsNullOrEmpty(m_IconTypes[i]) && m_Icons[i] != null)
				m_IconDictionary[m_IconTypes[i]] = m_Icons[i];
		}
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_NumPerRow = (int) (bounds.size.x / m_ItemSize.x);
		if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
			return;

		m_NumRows = (int) (bounds.size.z / m_ItemSize.z);

		m_StartPosition = bounds.extents.z * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

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

			if (count / m_NumPerRow + m_DataOffset < 0)
				RecycleGridItem(data);
			else if (count / m_NumPerRow + m_DataOffset > m_NumRows - 1)
				RecycleGridItem(data);
			else
				UpdateVisibleItem(data, count);

			count++;
		}
	}

	protected void RecycleGridItem(AssetData data)
	{
		if (!data.item)
			return;
		StartCoroutine(Transition(data, true));
	}

	private IEnumerator Transition(AssetData data, bool @out)
	{
		var startTime = Time.realtimeSinceStartup;
		var currTime = 0f;
		var cancel = false;

		var item = data.item;
		data.animating = true;

		var startVal = 0;
		var endVal = 1;
		if (@out)
		{
			data.item = null;
			startVal = 1;
			endVal = 0;
		}

		var lastScale = startVal * m_ScaleFactor;
		item.transform.localScale = Vector3.one * lastScale;
		while (currTime < kTransitionDuration)
		{
			if (!Mathf.Approximately(item.transform.localScale.x, lastScale))
			{
				cancel = true;
				break;
			}
			currTime = Time.realtimeSinceStartup - startTime;
			var t = currTime / kTransitionDuration;
			item.transform.localScale = Vector3.one * Mathf.Lerp(startVal, endVal, t * t) * m_ScaleFactor;
			lastScale = item.transform.localScale.x;
			yield return null;
		}
		if (!cancel)
		{
			if (@out)
			{
				m_TemplateDictionary[data.template].pool.Add(item);
				item.gameObject.SetActive(false);
			}
			item.transform.localScale = Vector3.one * m_ScaleFactor * endVal;
			data.animating = false;
		}
	}

	protected override void UpdateVisibleItem(AssetData data, int offset)
	{
		if (data.item == null)
			data.item = GetItem(data);
		UpdateGridItem(data, offset);
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

	private void UpdateGridItem(AssetData data, int offset)
	{
		var item = data.item as AssetGridItem;
		if (!data.animating)
			item.UpdateTransforms(m_ScaleFactor);

		var t = item.transform;
		var zOffset = m_ItemSize.z * (offset / m_NumPerRow) + m_ScrollOffset;
		var xOffset = m_ItemSize.x * (offset % m_NumPerRow);
		t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, kPositionFollow);
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData data)
	{
		// If this AssetData hasn't fetched its asset yet, do so now
		if (data.asset == null)
		{
			data.asset = EditorUtility.InstanceIDToObject(data.instanceID);
			data.preview = data.asset as GameObject;
		}

		var item = base.GetItem(data);

		item.transform.localPosition = m_StartPosition;
		connectInterfaces(item);

		StartCoroutine(Transition(data, false));

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
			AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(data.instanceID)), 
			texture => item.fallbackTexture = texture));
	}
}