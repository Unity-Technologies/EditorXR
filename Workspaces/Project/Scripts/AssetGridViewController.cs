using System;
using System.Collections;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.005f; // Give the cubes a margin so that their sides don't get clipped

	private const float kTransitionDuration = 0.1f;
	private const float kPositionFollow = 0.4f;

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	private int m_NumPerRow;

	public float scaleFactor
	{
		get { return m_ScaleFactor; }
		set { m_ScaleFactor = value; }
	}
	[SerializeField]
	private float m_ScaleFactor = 0.1f;

	protected override int dataLength { get { return Mathf.CeilToInt((float)base.dataLength / m_NumPerRow); } }

	public AssetData[] listData
	{
		set
		{
			if (m_Data != null) // Clear out visuals for old data
			{
				foreach (var data in m_Data)
				{
					CleanUpBeginning(data);
				}
			}
			m_Data = value;
		}
	}

	public Func<string, bool> testFilter; 

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<AssetGridItem>();
		item.GetMaterials(out m_TextMaterial);

		m_ScrollOffset = m_ScaleFactor;

		m_Data = new AssetData[0]; // Start with empty list to avoid null references
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_NumPerRow = (int) (bounds.size.x / m_ItemSize.x);
		if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
			return;

		m_NumRows = (int)(bounds.size.z / m_ItemSize.z);

		m_StartPosition = bounds.extents.z * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

		m_DataOffset = (int) (m_ScrollOffset / itemSize.z);
		if (m_ScrollOffset < 0)
			m_DataOffset --;

		m_ScrollReturn = float.MaxValue;

		// Snap back if list scrolled too far
		if (-m_DataOffset >= dataLength)
			m_ScrollReturn = (1 - dataLength) * itemSize.z;

		// Extend clip bounds slightly in Z for extra text
		var clipExtents = bounds.extents;
		clipExtents.z += kClipMargin;
		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", clipExtents);
	}

	protected override Vector3 GetObjectSize(GameObject g)
	{
		return g.GetComponent<BoxCollider>().size * m_ScaleFactor + Vector3.one * m_Padding;
	}

	protected override void UpdateItems()
	{
		int count = 0;
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
		float start = Time.realtimeSinceStartup;
		var currTime = 0f;
		bool cancel = false;

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

		float lastScale = startVal * m_ScaleFactor;
		item.transform.localScale = Vector3.one * lastScale;
		while (currTime < kTransitionDuration)
		{
			if (!Mathf.Approximately(item.transform.localScale.x, lastScale))
			{
				cancel = true;
				break;
			}
			currTime = Time.realtimeSinceStartup - start;
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
		{
			data.item = GetItem(data);
		}
		UpdateGridItem(data, offset);
	}

	public override void OnEndScrolling()
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
		if(!data.animating)
			item.UpdateTransforms(m_ScaleFactor);
		item.Clip(bounds, transform.worldToLocalMatrix);

		Transform t = item.transform;
		var zOffset = m_ItemSize.z * (offset / m_NumPerRow) + m_ScrollOffset;
		var xOffset = m_ItemSize.x * (offset % m_NumPerRow);
		t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, kPositionFollow);
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData data)
	{
		var item = base.GetItem(data);
		item.SwapMaterials(m_TextMaterial);
		StartCoroutine(Transition(data, false));
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}