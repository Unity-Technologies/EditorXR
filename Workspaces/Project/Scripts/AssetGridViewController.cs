using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.005f; //Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	private int m_RowCount;

	public AssetData[] listData { set { m_Data = value; } }
	public Bounds bounds { private get; set; }

	protected override void Setup()
	{
		base.Setup();
		var item = templates[0].GetComponent<AssetGridItem>();
		item.GetMaterials(out m_TextMaterial);

		m_Data = new AssetData[0]; //Start with empty list to avoid null references
	}

	protected override void ComputeConditions()
	{
		if (templates.Length > 0) {
			//Use first template to get item size
			m_ItemSize = GetObjectSize(templates[0]);
		}

		m_RowCount = (int) (bounds.size.x / m_ItemSize.x);

		//Resize range to nearest multiple of item width
		m_NumItems = m_RowCount * Mathf.RoundToInt(range / m_ItemSize.z); //Number of cards that will fit

		//Get initial conditions. This procedure is done every frame in case the collider bounds change at runtime
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward + (bounds.extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

		m_DataOffset = (int)(scrollOffset / itemSize.z) * m_RowCount;
		if (scrollOffset < 0)
			m_DataOffset--;

		//Extend clip bounds slightly in Z for extra text
		var clipExtents = bounds.extents;
		clipExtents.z += kClipMargin;
		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", clipExtents);
	}

	void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.DrawSphere(m_StartPosition, 0.05f);
	}

	protected override void Positioning(Transform t, int offset)
	{
		AssetGridItem item = t.GetComponent<AssetGridItem>();
		item.UpdateTransforms(bounds.size.x - kClipMargin);
		item.Clip(bounds, transform.worldToLocalMatrix);

		float zOffset = m_ItemSize.z * (offset / m_RowCount) + scrollOffset;
		float xOffset = m_ItemSize.x * (offset % m_RowCount);
		t.localPosition = m_StartPosition + (zOffset + scrollOffset) * Vector3.back + xOffset * Vector3.right;
		t.localRotation = Quaternion.identity;
	}

	protected override AssetGridItem GetItem(AssetData listData)
	{
		var item = (AssetGridItem)base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}