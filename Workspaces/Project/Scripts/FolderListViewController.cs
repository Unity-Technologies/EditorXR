using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class FolderListViewController : NestedListViewController<FolderData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;
	private Material m_ExpandArrowMaterial;

	private Transform m_GrabbedObject;

	public FolderData[] listData { set { m_Data = value; } }
	public Bounds bounds { private get; set; }

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<FolderListItem>();
		item.GetMaterials(out m_TextMaterial, out m_ExpandArrowMaterial);
	}

	protected override void ComputeConditions()
	{
		if (m_Templates.Length > 0)
		{
			// Use first template to get item size
			m_ItemSize = GetObjectSize(m_Templates[0]);
		}

		m_NumItems = Mathf.RoundToInt(bounds.size.z / m_ItemSize.z);
		
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

		m_DataOffset = (int) (m_ScrollOffset / itemSize.z);
		if (m_ScrollOffset < 0)
			m_DataOffset--;

		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", bounds.extents);
		m_ExpandArrowMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_ExpandArrowMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	protected override void UpdateItem(Transform t, int offset)
	{
		FolderListItem item = t.GetComponent<FolderListItem>();
		item.UpdateTransforms(bounds.size.x - kClipMargin);
		item.Clip(bounds, transform.worldToLocalMatrix);

		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + m_ScrollOffset) * Vector3.back;
		t.localRotation = Quaternion.identity;
	}

	protected override ListViewItem<FolderData> GetItem(FolderData listData)
	{
		var item = (FolderListItem) base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}