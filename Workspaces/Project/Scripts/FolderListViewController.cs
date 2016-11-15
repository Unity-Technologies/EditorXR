using ListView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class FolderListViewController : NestedListViewController<FolderData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	private Material m_TextMaterial;

	[SerializeField]
	private Material m_ExpandArrowMaterial;

	int m_SelectedFolder;

	readonly Dictionary<int, bool> m_ExpandStates = new Dictionary<int, bool>();

	public Action<FolderData> selectFolder { private get; set; }

	public override FolderData[] data
	{
		set
		{
			if (m_Data != null)
			{
				// Clear out visuals for old data
				foreach (var datum in m_Data)
				{
					RecycleRecursively(datum);
				}
			}

			m_Data = value;
			if (m_SelectedFolder != 0 && m_Data.Length > 0)
				SelectFolder(GetFolderDataByInstanceID(m_Data[0], m_SelectedFolder));
		}
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

	void UpdateFolderItem(FolderData data, int offset, int depth, bool expanded)
	{
		if (data.item == null)
			data.item = GetItem(data);
		var item = (FolderListItem)data.item;
		item.UpdateSelf(bounds.size.x - kClipMargin, depth, expanded, data.instanceID == m_SelectedFolder);

		SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItem(item.transform, offset);
	}

	protected override void UpdateRecursively(FolderData[] data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			bool expanded;
			if (!m_ExpandStates.TryGetValue(datum.instanceID, out expanded))
			{
				expanded = datum.defaultToExpanded;
				m_ExpandStates[datum.instanceID] = expanded;
			}

			if (count + m_DataOffset < -1)
				RecycleBeginning(datum);
			else if (count + m_DataOffset > m_NumRows - 1)
				RecycleEnd(datum);
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

	protected override ListViewItem<FolderData> GetItem(FolderData listData)
	{
		var item = (FolderListItem)base.GetItem(listData);
		item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		item.selectFolder = SelectFolder;

		item.toggleExpanded = ToggleExpanded;

		return item;
	}

	void ToggleExpanded(FolderData data)
	{
		var instanceID = data.instanceID;
		m_ExpandStates[instanceID] = !m_ExpandStates[instanceID];
	}

	void SelectFolder(FolderData data)
	{
		m_SelectedFolder = data.instanceID;
		selectFolder(data);
	}

	FolderData GetFolderDataByInstanceID(FolderData data, int instanceID)
	{
		if (data.instanceID == instanceID)
			return data;

		if (data.children != null)
		{
			foreach (var child in data.children)
			{
				var folder = GetFolderDataByInstanceID(child, instanceID);
				if (folder != null)
					return folder;
			}
		}
		return null;
	}

	private void OnDestroy()
	{
		U.Object.Destroy(m_TextMaterial);
		U.Object.Destroy(m_ExpandArrowMaterial);
	}
}