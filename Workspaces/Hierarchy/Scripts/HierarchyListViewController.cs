using System;
using System.Collections.Generic;
using ListView;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Utilities;

public class HierarchyListViewController : NestedListViewController<HierarchyData>
{
	const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	BaseHandle m_TopDropZone;

	[SerializeField]
	Material m_TextMaterial;

	[SerializeField]
	Material m_ExpandArrowMaterial;

	[SerializeField]
	Material m_NoClipBackingCubeMaterial;

	Material m_TopDropZoneMaterial;
	float m_TopDropZoneAlpha;

	int m_SelectedRow;

	readonly Dictionary<int, bool> m_ExpandStates = new Dictionary<int, bool>();

	public Action<int> selectRow { private get; set; }

	protected override void Setup()
	{
		base.Setup();

		m_TextMaterial = Instantiate(m_TextMaterial);
		m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
		m_NoClipBackingCubeMaterial = Instantiate(m_NoClipBackingCubeMaterial);

		m_TopDropZoneMaterial = U.Material.GetMaterialClone(m_TopDropZone.GetComponent<Renderer>());
		var color = m_TopDropZoneMaterial.color;
		m_TopDropZoneAlpha = color.a;
		color.a = 0;
		m_TopDropZoneMaterial.color = color;

		m_TopDropZone.canDrop += CanDrop;
		m_TopDropZone.receiveDrop += RecieveDrop;
		m_TopDropZone.dropHoverStarted += DropHoverStarted;
		m_TopDropZone.dropHoverEnded += DropHoverEnded;
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

		var width = bounds.size.x - kClipMargin;
		var dropZoneTransform = m_TopDropZone.transform;
		var dropZoneScale = dropZoneTransform.localScale;
		dropZoneScale.x = width;
		dropZoneTransform.localScale = dropZoneScale;

		var dropZonePosition = dropZoneTransform.localPosition;
		dropZonePosition.z = bounds.extents.z;
		dropZoneTransform.localPosition = dropZonePosition;

		var hierarchyItem = (HierarchyListItem)item;
		hierarchyItem.UpdateSelf(width, depth, expanded, data.instanceID == m_SelectedRow);

		SetMaterialClip(hierarchyItem.cubeMaterial, transform.worldToLocalMatrix);
		SetMaterialClip(hierarchyItem.dropZoneMaterial, transform.worldToLocalMatrix);

		UpdateItemTransform(item.transform, offset);
	}

	protected override void UpdateRecursively(List<HierarchyData> data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			var instanceID = datum.instanceID;
			bool expanded;
			if (!m_ExpandStates.TryGetValue(instanceID, out expanded))
				m_ExpandStates[instanceID] = false;

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
			else
			{
				m_ExpandStates[instanceID] = false;
			}
		}
	}

	protected override ListViewItem<HierarchyData> GetItem(HierarchyData listData)
	{
		var item = (HierarchyListItem)base.GetItem(listData);
		item.SetMaterials(m_NoClipBackingCubeMaterial, m_TextMaterial, m_ExpandArrowMaterial);
		item.selectRow = SelectRow;

		item.toggleExpanded = ToggleExpanded;

		item.isExpanded = GetExpanded;

		item.UpdateArrow(GetExpanded(listData.instanceID), true);

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

		var scrollHeight = 0f;
		foreach (var datum in data)
		{
			ScrollToRow(datum, instanceID, ref scrollHeight);
			scrollHeight += itemSize.z;
		}
	}

	bool ExpandToRow(HierarchyData container, int rowID)
	{
		if (container.instanceID == rowID)
			return true;

		var found = false;
		if (container.children != null)
		{
			foreach (var child in container.children)
			{
				if (ExpandToRow(child, rowID))
					found = true;
			}
		}

		if (found)
			m_ExpandStates[container.instanceID] = true;

		return found;
	}

	void ScrollToRow(HierarchyData container, int rowID, ref float scrollHeight)
	{
		if (container.instanceID == rowID)
		{
			if (-scrollOffset > scrollHeight || -scrollOffset + bounds.size.z < scrollHeight)
				scrollOffset = -scrollHeight;
			return;
		}

		if (container.children != null)
		{
			foreach (var child in container.children)
			{
				if (GetExpanded(container.instanceID))
				{
					ScrollToRow(child, rowID, ref scrollHeight);
					scrollHeight += itemSize.z;
				}
			}
		}
	}

	bool CanDrop(BaseHandle handle, object dropObject)
	{
		return dropObject is HierarchyData && dropObject != data[0];
	}

	static void RecieveDrop(BaseHandle handle, object dropObject) {
		var hierarchyData = dropObject as HierarchyData;
		if (hierarchyData != null)
		{
			var gameObject = EditorUtility.InstanceIDToObject(hierarchyData.instanceID) as GameObject;
			gameObject.transform.SetParent(null);
			gameObject.transform.SetSiblingIndex(0);
		}
	}

	void DropHoverStarted(BaseHandle handle)
	{
		var color = m_TopDropZoneMaterial.color;
		color.a = m_TopDropZoneAlpha;
		m_TopDropZoneMaterial.color = color;
	}

	void DropHoverEnded(BaseHandle handle)
	{
		var color = m_TopDropZoneMaterial.color;
		color.a = 0;
		m_TopDropZoneMaterial.color = color;
	}

	bool GetExpanded(int instanceID)
	{
		bool expanded;
		m_ExpandStates.TryGetValue(instanceID, out expanded);
		return expanded;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
		U.Object.Destroy(m_NoClipBackingCubeMaterial);
	}
}