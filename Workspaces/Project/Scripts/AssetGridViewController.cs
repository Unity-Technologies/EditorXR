using System;
using System.Collections;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.005f; // Give the cubes a margin so that their sides don't get clipped

	private const float kRecycleDuration = 0.1f;
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
			if (m_Data != null)
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

		m_Data = new AssetData[0]; // Start with empty list to avoid null references
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_NumPerRow = (int) (bounds.size.x / m_ItemSize.x);
		if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
			return;

		m_NumRows = Mathf.CeilToInt(bounds.size.z / m_ItemSize.z);

		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

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
				CleanUpBeginning(data);
				continue;
			}

			if (!testFilter(data.type)) // If this item doesn't match the filter, move on to the next item; do not count
			{
				CleanUpBeginning(data);
				continue;
			}

			if (count / m_NumPerRow + m_DataOffset < 0)
				CleanUpBeginning(data);
			else if (count / m_NumPerRow + m_DataOffset > m_NumRows - 2)
				CleanUpEnd(data);
			else
				UpdateVisibleItem(data, count);

			count++;
		}
	}

	protected override void RecycleItem(string template, MonoBehaviour item)
	{
		if (item == null || template == null)
			return;
		StartCoroutine(AnimateOut(template, item));
	}

	private IEnumerator AnimateOut(string template, MonoBehaviour item)
	{
		float start = Time.realtimeSinceStartup;
		//Quaternion startRot = item.transform.rotation;
		//Vector3 startPos = item.transform.position;
		float startScale = item.transform.localScale.x;
		while (Time.realtimeSinceStartup - start < kRecycleDuration)
		{
			//item.transform.rotation = Quaternion.Lerp(startRot, destination.rotation, (Time.time - start) / kRecycleDuration);
			//item.transform.position = Vector3.Lerp(startPos, destination.position, (Time.time - start) / kRecycleDuration);
			var t = (Time.realtimeSinceStartup - start) / kRecycleDuration;
			item.transform.localScale = Vector3.one * Mathf.Lerp(startScale, 0, t * t);
			yield return null;
		}
		//item.transform.rotation = destination.rotation;
		//item.transform.position = destination.position;
		m_TemplateDictionary[template].pool.Add(item);
		item.gameObject.SetActive(false);
		item.transform.localScale = Vector3.one * startScale;
	}

	private IEnumerator AnimateIn(AssetData data)
	{
		float start = Time.realtimeSinceStartup;
		float startScale = data.item.transform.localScale.x;
		data.item.transform.localScale = Vector3.zero;
		data.animating = true;
		var item = data.item;

		while (Time.realtimeSinceStartup - start < kRecycleDuration)
		{
			if(!item || !item.gameObject.activeSelf)
				yield break;

			var t = (Time.realtimeSinceStartup - start) / kRecycleDuration;
			data.item.transform.localScale = Vector3.one * Mathf.Lerp(0, startScale, t * t);
			yield return null;
		}
		data.animating = false;
	}

	protected override void UpdateVisibleItem(AssetData data, int offset)
	{
		if (data.item == null)
		{
			data.item = GetItem(data);
		}
		UpdateItem(data.item.transform, offset, data.animating);
	}

	private void UpdateItem(Transform t, int offset, bool animating)
	{
		AssetGridItem item = t.GetComponent<AssetGridItem>();
		if(!animating)
			item.UpdateTransforms(m_ScaleFactor);
		item.Clip(bounds, transform.worldToLocalMatrix);

		var zOffset = m_ItemSize.z * (offset / m_NumPerRow) + m_ScrollOffset;
		var xOffset = m_ItemSize.x * (offset % m_NumPerRow);
		t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, kPositionFollow);
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData data)
	{
		var item = base.GetItem(data);
		item.SwapMaterials(m_TextMaterial);
		StartCoroutine(AnimateIn(data));
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}