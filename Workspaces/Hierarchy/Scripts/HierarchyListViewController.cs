using System;
using System.Collections.Generic;
using ListView;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

public class HierarchyListViewController : NestedListViewController<HierarchyData>
{
	const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	Material m_TextMaterial;

	[SerializeField]
	Material m_ExpandArrowMaterial;

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

	void UpdateHierarchyItem(HierarchyData data, int offset, int depth, bool expanded)
	{
		ListViewItem<HierarchyData> item;
		if (!m_ListItems.TryGetValue(data, out item))
			item = GetItem(data);

		var hierarchyItem = (HierarchyListItem)item;

		hierarchyItem.UpdateSelf(bounds.size.x - kClipMargin, depth, expanded, data.instanceID == m_SelectedRow);

		SetMaterialClip(hierarchyItem.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItemTransform(item.transform, offset);
	}

	protected override void UpdateRecursively(List<HierarchyData> data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			bool expanded;
			if (!m_ExpandStates.TryGetValue(datum.instanceID, out expanded))
				m_ExpandStates[datum.instanceID] = false;

			if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
				Recycle(datum);
			else
				UpdateHierarchyItem(datum, count, depth, expanded);

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
			item.UpdateArrow(expanded, true);

		return item;
	}

	void ToggleExpanded(HierarchyData data)
	{
		var instanceID = data.instanceID;
		m_ExpandStates[instanceID] = !m_ExpandStates[instanceID];
	}

	public void SelectRow(int instanceID)
	{
		if (data == null)
			return;

		m_SelectedRow = instanceID;

		foreach (var datum in data)
		{
			ExpandToRow(datum, instanceID);
		}

		selectRow(instanceID);
	}

	bool ExpandToRow(HierarchyData container, int rowID, float scrollHeight = 0)
	{
		if (container.instanceID == rowID)
		{
			scrollOffset = -scrollHeight - itemSize.z;
			return true;
		}

		scrollHeight += itemSize.z;

		bool found = false;

		if (container.children != null)
		{
			foreach (var child in container.children)
			{
				if (ExpandToRow(child, rowID, scrollHeight))
					found = true;
			}
		}

		if (found)
			m_ExpandStates[container.instanceID] = true;

		return found;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}