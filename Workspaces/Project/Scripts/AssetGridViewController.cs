using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.005f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	private int m_RowCount;

	float m_ScrollReturn = float.MaxValue;

	public AssetData[] listData { set { m_Data = value; } }

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<AssetGridItem>();
		item.GetMaterials(out m_TextMaterial);

		m_Data = new AssetData[0]; // Start with empty list to avoid null references
	}

	protected override void ComputeConditions()
	{
		if (m_Templates.Length > 0)
		{
			// Use first template to get item size
			m_ItemSize = GetObjectSize(m_Templates[0]);
		}

		m_RowCount = (int) (bounds.size.x / m_ItemSize.x);
		
		m_NumItems = m_RowCount * Mathf.RoundToInt(bounds.size.z / m_ItemSize.z);
		
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

		m_DataOffset = (int) (m_ScrollOffset / itemSize.z) * m_RowCount;
		if (m_ScrollOffset < 0)
			m_DataOffset--;

		// Extend clip bounds slightly in Z for extra text
		var clipExtents = bounds.extents;
		clipExtents.z += kClipMargin;
		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", clipExtents);
	}

	protected override void UpdateItem(Transform t, int offset)
	{
		AssetGridItem item = t.GetComponent<AssetGridItem>();
		item.UpdateTransforms();
		item.Clip(bounds, transform.worldToLocalMatrix);

		float zOffset = m_ItemSize.z * (offset / m_RowCount) + m_ScrollOffset;
		float xOffset = m_ItemSize.x * (offset % m_RowCount);
		t.localPosition = m_StartPosition + (zOffset + m_ScrollOffset) * Vector3.back + xOffset * Vector3.right;
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData listData)
	{
		var item = (AssetGridItem) base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}