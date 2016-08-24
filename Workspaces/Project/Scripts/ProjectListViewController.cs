using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class ProjectListViewController : NestedListViewController<AssetData>
{
	public AssetData[] data {set { m_Data = value;}}
	public Bounds bounds { private get; set; }

	private Material m_TextMaterial;
	private Material m_ExpandArrowMaterial;

	protected override void Setup()
	{
		base.Setup();
		var item = templates[0].GetComponent<AssetListItem>();
		item.GetMaterials(out m_TextMaterial, out m_ExpandArrowMaterial);
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
		m_ExpandArrowMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_ExpandArrowMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	void OnDrawGizmos()
	{
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(bounds.center, bounds.size);
		Gizmos.DrawSphere(m_StartPosition, 0.01f);
	}

	protected override void Positioning(Transform t, int offset)
	{
		AssetListItem item = t.GetComponent<AssetListItem>();
		item.Resize(bounds.size.x);
		item.Clip(bounds, transform.worldToLocalMatrix);

		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.back;
		t.localRotation = Quaternion.identity;
	}

	protected override ListViewItem<AssetData> GetItem(AssetData data)
	{
		var item = (AssetListItem)base.GetItem(data);
		item.SwapMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}