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
		var item = templates[0].GetComponent<FolderListItem>();
		item.GetMaterials(out m_TextMaterial, out m_ExpandArrowMaterial);
	}

	protected override void ComputeConditions()
	{
		if (templates.Length > 0)
		{
			// Use first template to get item size
			m_ItemSize = GetObjectSize(templates[0]);
		}

		m_NumItems = Mathf.RoundToInt(range / m_ItemSize.z);
		
		m_StartPosition = (bounds.extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

		m_DataOffset = (int) (scrollOffset / itemSize.z);
		if (scrollOffset < 0)
			m_DataOffset--;

		var parentMatrix = transform.worldToLocalMatrix;
		m_TextMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_TextMaterial.SetVector("_ClipExtents", bounds.extents);
		m_ExpandArrowMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_ExpandArrowMaterial.SetVector("_ClipExtents", bounds.extents);
	}

	protected override void Positioning(Transform t, int offset)
	{
		FolderListItem item = t.GetComponent<FolderListItem>();
		item.UpdateTransforms(bounds.size.x - kClipMargin);
		item.Clip(bounds, transform.worldToLocalMatrix);

		t.localPosition = m_StartPosition + (offset * m_ItemSize.z + scrollOffset) * Vector3.back;
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