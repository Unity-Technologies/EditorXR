using System;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class FolderListViewController : NestedListViewController<FolderData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	private Material m_TextMaterial;

	private Transform m_GrabbedObject;

	public Action<FolderData> selectFolder;

	public void ClearSelected()
	{
		foreach (var folderData in m_Data)
			folderData.ClearSelected();
	}

	protected override void Setup()
	{
		base.Setup();
		var item = m_Templates[0].GetComponent<FolderListItem>();
		item.GetMaterials(out m_TextMaterial);
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		m_StartPosition = (bounds.extents.y - itemSize.y * 0.5f) * Vector3.up;

		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_TextMaterial, parentMatrix);
	}

	protected override void UpdateItemRecursive(FolderData data, int offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (FolderListItem)data.item;
		item.UpdateTransforms(bounds.size.x - kClipMargin, depth);
		SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItem(item.transform, offset);
	}

	protected override void UpdateItem(Transform t, int offset)
	{
		t.localPosition = m_StartPosition + (offset * m_ItemSize.y + m_ScrollOffset) * Vector3.down;
		t.localRotation = Quaternion.identity;
	}

	protected override ListViewItem<FolderData> GetItem(FolderData listData)
	{
		var item = (FolderListItem)base.GetItem(listData);
		item.SwapMaterials(m_TextMaterial);
		item.selectFolder = selectFolder;
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
	}
}