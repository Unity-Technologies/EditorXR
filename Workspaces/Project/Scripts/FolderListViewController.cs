using ListView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

public class FolderListViewController : NestedListViewController<FolderData>
{
	private const float kClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

	[SerializeField]
	private Material m_TextMaterial;

	[SerializeField]
	private Material m_ExpandArrowMaterial;

	string m_SelectedFolder;

	readonly Dictionary<string, bool> m_ExpandStates = new Dictionary<string, bool>();

	public Action<FolderData> selectFolder { private get; set; }

	public override List<FolderData> data
	{
		set
		{
			base.data = value;

			if (m_Data != null && m_Data.Count > 0) // Expand and select the Assets folder by default
			{
				var guid = data[0].guid;
				m_ExpandStates[guid] = true;
				SelectFolder(guid);
			}
		}
	}

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

	void UpdateFolderItem(FolderData data, int offset, int depth, bool expanded)
	{
		ListViewItem<FolderData> item;
		if (!m_ListItems.TryGetValue(data, out item))
			item = GetItem(data);

		var folderItem = (FolderListItem)item;

		folderItem.UpdateSelf(bounds.size.x - kClipMargin, depth, expanded, data.guid == m_SelectedFolder);

		SetMaterialClip(folderItem.cubeMaterial, transform.worldToLocalMatrix);

		UpdateItemTransform(item.transform, offset);
	}

	protected override void UpdateRecursively(List<FolderData> data, ref int count, int depth = 0)
	{
		foreach (var datum in data)
		{
			bool expanded;
			if (!m_ExpandStates.TryGetValue(datum.guid, out expanded))
				m_ExpandStates[datum.guid] = false;

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

	protected override ListViewItem<FolderData> GetItem(FolderData listData)
	{
		var item = (FolderListItem)base.GetItem(listData);
		item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
		item.selectFolder = SelectFolder;

		item.toggleExpanded = ToggleExpanded;

		bool expanded;
		if(m_ExpandStates.TryGetValue(listData.guid, out expanded))
			item.UpdateArrow(expanded, true);

		return item;
	}

	void ToggleExpanded(FolderData data)
	{
		var guid = data.guid;
		m_ExpandStates[guid] = !m_ExpandStates[guid];
	}

	void SelectFolder(string guid)
	{
		if (data == null)
			return;

		m_SelectedFolder = guid;

		var folderData = GetFolderDataByGUID(data[0], guid) ?? data[0];
		selectFolder(folderData);
	}

	static FolderData GetFolderDataByGUID(FolderData data, string guid)
	{
		if (data.guid == guid)
			return data;

		if (data.children != null)
		{
			foreach (var child in data.children)
			{
				var folder = GetFolderDataByGUID(child, guid);
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