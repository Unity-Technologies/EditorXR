using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class AssetGridViewController : ListViewController<AssetData, AssetGridItem>
{
	private const float kClipMargin = 0.001f; //Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	public AssetData[] listData { set { m_Data = value; } }
	public Bounds bounds { private get; set; }

	protected override void Setup()
	{
		base.Setup();
		var item = templates[0].GetComponent<AssetGridItem>();
		item.GetMaterials(out m_TextMaterial);
	}

	protected override void ComputeConditions()
	{
		if (templates.Length > 0) {
			//Use first template to get item size
			m_ItemSize = GetObjectSize(templates[0]);
		}
		//Resize range to nearest multiple of item width
		m_NumItems = Mathf.RoundToInt(range / m_ItemSize.z); //Number of cards that will fit
		range = m_NumItems * m_ItemSize.z;

		//Get initial conditions. This procedure is done every frame in case the collider bounds change at runtime
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

		m_DataOffset = (int)(scrollOffset / itemSize.z);
		if (scrollOffset < 0)
			m_DataOffset--;

		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	protected override void Positioning(Transform t, int offset)
	{
		AssetGridItem item = t.GetComponent<AssetGridItem>();
		item.Resize(bounds.size.x - kClipMargin);
		item.Clip(bounds, transform.worldToLocalMatrix);

		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.back;
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