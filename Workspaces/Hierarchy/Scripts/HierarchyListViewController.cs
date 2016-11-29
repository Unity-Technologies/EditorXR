using System;
using System.Collections.Generic;
using ListView;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class HierarchyListViewController : NestedListViewController<HierarchyData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	private Material m_TextMaterial;

	[SerializeField]
	private Material m_ExpandArrowMaterial;

	int m_SelectedRow;

	readonly Dictionary<int, bool> m_ExpandStates = new Dictionary<int, bool>();

	public Action<int> selectRow;

	protected override void Setup()
	{
		base.Setup();

		m_TextMaterial = Instantiate(m_TextMaterial);
		m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
	}

	protected override void UpdateItems()
	{
		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_TextMaterial, parentMatrix);
		SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);

		base.UpdateItems();
	}

	void UpdateFolderItem(HierarchyData data, int offset, int depth, bool expanded)
	{
		ListViewItem<HierarchyData> item;
		if (!m_ListItems.TryGetValue(data, out item))
			item = GetItem(data);

		var folderItem = (HierarchyListItem)item;

		folderItem.UpdateSelf(bounds.size.x - kClipMargin, depth, expanded, data.instanceID == m_SelectedRow);

		SetMaterialClip(folderItem.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItemTransform(item.transform, offset);
	}

	protected override void UpdateRecursively(HierarchyData[] data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			bool expanded;
			if (!m_ExpandStates.TryGetValue(datum.instanceID, out expanded))
				m_ExpandStates[datum.instanceID] = false;

			if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
				Recycle(datum);
			else
				UpdateFolderItem(datum, count, depth, expanded);

			count++;

			if (datum.children != null)
			{
				if (expanded)
					UpdateRecursively(datum.children, ref count, depth + 1);
				else
					RecycleChildren(datum);
			}
		}
	}

	protected override ListViewItem<HierarchyData> GetItem(HierarchyData listData)
	{
		var item = (HierarchyListItem)base.GetItem(listData);
		item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		item.selectRow = SelectRow;

		item.toggleExpanded = ToggleExpanded;

		bool expanded;
		if(m_ExpandStates.TryGetValue(listData.instanceID, out expanded))
			item.UpdateArrow(m_ExpandStates[listData.instanceID], true);

		return item;
	}

	void ToggleExpanded(HierarchyData data)
	{
		var instanceID = data.instanceID;
		m_ExpandStates[instanceID] = !m_ExpandStates[instanceID];
	}

	void SelectRow(int instanceID)
	{
		if (data == null)
			return;

		m_SelectedRow = instanceID;

		selectRow(instanceID);
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}