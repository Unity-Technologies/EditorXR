using ListView;
using System;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class HierarchyListViewController : NestedListViewController<HierarchyData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	private Material m_TextMaterial;

	[SerializeField]
	private Material m_ExpandArrowMaterial;

	private Transform m_GrabbedObject;

	public Action<HierarchyData> selectRow;

	public override HierarchyData[] data
	{
		set
		{
			if (m_Data != null)
			{
				// Clear out visuals for old data
				foreach (var data in m_Data)
				{
					RecycleRecursively(data);
				}
			}

			m_Data = value;
		}
	}

	public void ClearSelected()
	{
		foreach (var folderData in m_Data)
			folderData.ClearSelected();
	}

	protected override void Setup()
	{
		base.Setup();

		m_TextMaterial = Instantiate(m_TextMaterial);
		m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
	}

	protected override void ComputeConditions()
	{
		base.ComputeConditions();

		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_TextMaterial, parentMatrix);
		SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);
	}

	protected override void UpdateNestedItem(HierarchyData data, int offset, int depth)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (HierarchyItem)data.item;
		item.UpdateSelf(bounds.size.x - kClipMargin, depth);

		SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItem(item.transform, offset);
	}

	protected override ListViewItem<HierarchyData> GetItem(HierarchyData listData)
	{
		var item = (HierarchyItem)base.GetItem(listData);
		item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		item.selectRow = selectRow;
		return item;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}