using ListView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Handles;
using UnityEngine.Experimental.EditorVR.Utilities;

class HierarchyListViewController : NestedListViewController<HierarchyData, int>
{
	const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	BaseHandle m_TopDropZone;

	[SerializeField]
	BaseHandle m_BottomDropZone;

	[SerializeField]
	Material m_TextMaterial;

	[SerializeField]
	Material m_ExpandArrowMaterial;

	[SerializeField]
	float m_SettleSpeed = 0.4f;

	[SerializeField]
	float m_SettleDuration = 1f;

	bool m_Settling;
	Action m_OnSettlingComplete;

	Material m_TopDropZoneMaterial;
	Material m_BottomDropZoneMaterial;
	float m_DropZoneAlpha;
	float m_BottomDropZoneStartHeight;

	int m_SelectedRow;

	readonly Dictionary<int, bool> m_GrabbedRows = new Dictionary<int, bool>();

	public Action<int> selectRow { private get; set; }

	public override List<HierarchyData> data
	{
		get { return base.data; }
		set
		{
			m_Data = value;

			// Update visible rows
			foreach (var row in m_ListItems)
			{
				var newData = m_Data.FirstOrDefault(data => data.index == row.Key);
				if (newData != null)
					row.Value.data = newData;
			}
		}
	}

	protected override void Setup()
	{
		base.Setup();

		m_TextMaterial = Instantiate(m_TextMaterial);
		m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);

		m_BottomDropZoneMaterial = U.Material.GetMaterialClone(m_BottomDropZone.GetComponent<Renderer>());
		m_BottomDropZoneStartHeight = m_BottomDropZone.transform.localScale.z;
		m_TopDropZoneMaterial = U.Material.GetMaterialClone(m_TopDropZone.GetComponent<Renderer>());
		var color = m_TopDropZoneMaterial.color;
		m_DropZoneAlpha = color.a;
		color.a = 0;
		m_TopDropZoneMaterial.color = color;
		m_BottomDropZoneMaterial.color = color;

		var dropZones = new []{ m_BottomDropZone, m_TopDropZone };
		foreach (var dropZone in dropZones)
		{
			dropZone.canDrop += CanDrop;
			dropZone.receiveDrop += RecieveDrop;
			dropZone.dropHoverStarted += DropHoverStarted;
			dropZone.dropHoverEnded += DropHoverEnded;
		}
	}

	protected override void UpdateItems()
	{
		var parentMatrix = transform.worldToLocalMatrix;
		SetMaterialClip(m_TextMaterial, parentMatrix);
		SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);

		base.UpdateItems();

		// Update Drop Zones
		var width = bounds.size.x - kClipMargin;
		var dropZoneTransform = m_TopDropZone.transform;
		var dropZoneScale = dropZoneTransform.localScale;
		dropZoneScale.x = width;
		dropZoneTransform.localScale = dropZoneScale;

		var dropZonePosition = dropZoneTransform.localPosition;
		dropZonePosition.z = bounds.extents.z;
		dropZoneTransform.localPosition = dropZonePosition;

		dropZoneTransform = m_BottomDropZone.transform;
		dropZoneScale = dropZoneTransform.localScale;
		dropZoneScale.x = width;
		dropZoneScale.z = bounds.size.z - m_ExpandedDataLength * m_ItemSize.Value.z;
		dropZoneTransform.localScale = dropZoneScale;

		dropZonePosition = dropZoneTransform.localPosition;
		dropZonePosition.z -= bounds.extents.z;
		dropZoneTransform.localPosition = dropZonePosition;
	}

	void UpdateHierarchyItem(HierarchyData data, int offset, int depth, bool expanded)
	{
		var index = data.index;
		ListViewItem<HierarchyData, int> item;
		if (!m_ListItems.TryGetValue(index, out item))
			item = GetItem(data);

		var hierarchyItem = (HierarchyListItem)item;
		var width = bounds.size.x - kClipMargin;
		hierarchyItem.UpdateSelf(width, depth, expanded, data.index == m_SelectedRow);

		SetMaterialClip(hierarchyItem.cubeMaterial, transform.worldToLocalMatrix);
		SetMaterialClip(hierarchyItem.dropZoneMaterial, transform.worldToLocalMatrix);

		UpdateHierarchyItemTransform(item.transform, offset);
	}

	void UpdateHierarchyItemTransform(Transform t, int offset)
	{
		var destination = m_StartPosition + (offset * m_ItemSize.Value.z + m_ScrollOffset) * Vector3.back;
		t.localPosition = Vector3.Lerp(t.localPosition, destination, m_Settling ? m_SettleSpeed : 1);
		t.localRotation = Quaternion.identity;
	}

	protected override void UpdateRecursively(List<HierarchyData> data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			var index = datum.index;
			bool expanded;
			if (!m_ExpandStates.TryGetValue(index, out expanded))
				m_ExpandStates[index] = false;

			bool grabbed;
			if (!m_GrabbedRows.TryGetValue(index, out grabbed))
				m_GrabbedRows[index] = false;

			if (grabbed)
				continue;

			if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
				Recycle(index);
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
				m_ExpandStates[index] = false;
			}
		}
	}

	protected override ListViewItem<HierarchyData, int> GetItem(HierarchyData data)
	{
		var item = (HierarchyListItem)base.GetItem(data);
		item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		item.selectRow = SelectRow;

		item.toggleExpanded = ToggleExpanded;
		item.setExpanded = SetExpanded;
		item.startSettling = StartSettling;
		item.isExpanded = GetExpanded;
		item.setRowGrabbed = SetRowGrabbed;
		item.getListItem = GetListItem;

		item.UpdateArrow(GetExpanded(data.index), true);

		return item;
	}

	void ToggleExpanded(int instanceID)
	{
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
		var index = container.index;
		if (index == rowID)
		{
			return true;
		}

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
			m_ExpandStates[index] = true;

		return found;
	}

	void ScrollToRow(HierarchyData container, int rowID, ref float scrollHeight)
	{
		var index = container.index;
		if (index == rowID)
		{
			if (-scrollOffset > scrollHeight || -scrollOffset + bounds.size.z < scrollHeight)
				scrollOffset = -scrollHeight;
			return;
		}

		if (container.children != null)
		{
			foreach (var child in container.children)
			{
				if (GetExpanded(index))
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

	void RecieveDrop(BaseHandle handle, object dropObject)
	{
		if (handle == m_TopDropZone)
		{
			var hierarchyData = dropObject as HierarchyData;
			if (hierarchyData != null)
			{
				var gameObject = EditorUtility.InstanceIDToObject(hierarchyData.index) as GameObject;
				gameObject.transform.SetParent(null);
				gameObject.transform.SetSiblingIndex(0);
				StartSettling(null);
			}
		}

	}

	void DropHoverStarted(BaseHandle handle)
	{
		var material = handle == m_TopDropZone ? m_TopDropZoneMaterial : m_BottomDropZoneMaterial;
		var color = material.color;
		color.a = m_DropZoneAlpha;
		material.color = color;
	}

	void DropHoverEnded(BaseHandle handle)
	{
		var material = handle == m_TopDropZone ? m_TopDropZoneMaterial : m_BottomDropZoneMaterial;
		var color = material.color;
		color.a = 0;
		material.color = color;
	}

	bool GetExpanded(int instanceID)
	{
		bool expanded;
		m_ExpandStates.TryGetValue(instanceID, out expanded);
		return expanded;
	}

	void SetExpanded(int instanceID, bool expanded)
	{
		m_ExpandStates[instanceID] = expanded;
	}

	void StartSettling(Action onComplete)
	{
		if (m_OnSettlingComplete != null)
		{
			m_OnSettlingComplete();
			StopAllCoroutines();
		}

		m_OnSettlingComplete = onComplete;
		StartCoroutine(SettlingTimer());
	}

	IEnumerator SettlingTimer()
	{
		m_Settling = true;
		var startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < m_SettleDuration)
			yield return null;
		m_Settling = false;

		if (m_OnSettlingComplete != null)
		{
			m_OnSettlingComplete();
			m_OnSettlingComplete = null;
		}
	}

	void SetRowGrabbed(int index, bool grabbed)
	{
		m_GrabbedRows[index] = grabbed;
	}

	HierarchyListItem GetListItem(int index)
	{
		ListViewItem<HierarchyData, int> item;
		if (m_ListItems.TryGetValue(index, out item))
			return (HierarchyListItem)item;
		return null;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}