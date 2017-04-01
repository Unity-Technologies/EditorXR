#if UNITY_EDITOR
using ListView;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class FolderListViewController : NestedListViewController<FolderData, FolderListItem, string>
	{
		const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

		[SerializeField]
		Material m_TextMaterial;

		[SerializeField]
		Material m_ExpandArrowMaterial;

		readonly Dictionary<string, bool> m_ExpandStates = new Dictionary<string, bool>();

		public string selectedFolder { get; private set; }

		public Action<FolderData> selectFolder { private get; set; }

		public override List<FolderData> data
		{
			set
			{
				base.data = value;

				if (m_Data != null && m_Data.Count > 0) // Expand and select the Assets folder by default
				{
					var guid = data[0].index;
					m_ExpandStates[guid] = true;
					SelectFolder(selectedFolder ?? guid);
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

		void UpdateFolderItem(FolderData data, float offset, int depth, bool expanded, ref bool doneSettling)
		{
			var index = data.index;
			FolderListItem item;
			if (!m_ListItems.TryGetValue(index, out item))
				item = GetItem(data);

			item.UpdateSelf(bounds.size.x - k_ClipMargin, depth, expanded, index == selectedFolder);

			SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix);

			UpdateItem(item.transform, offset, ref doneSettling);
		}

		protected override void UpdateRecursively(List<FolderData> data, ref float offset, ref bool doneSettling, int depth = 0)
		{
			for (int i = 0; i < data.Count; i++)
			{
				var datum = data[i];
				var index = datum.index;
				bool expanded;
				if (!m_ExpandStates.TryGetValue(index, out expanded))
					m_ExpandStates[index] = false;

				if (offset + scrollOffset + itemSize.z < 0 || offset + scrollOffset > bounds.size.z)
					Recycle(index);
				else
					UpdateFolderItem(datum, offset + m_ScrollOffset, depth, expanded, ref doneSettling);

				offset += itemSize.z;

				if (datum.children != null)
				{
					if (expanded)
						UpdateRecursively(datum.children, ref offset, ref doneSettling, depth + 1);
					else
						RecycleChildren(datum);
				}
			}
		}

		protected override FolderListItem GetItem(FolderData listData)
		{
			var item = base.GetItem(listData);
			item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
			item.selectFolder = SelectFolder;

			item.toggleExpanded = ToggleExpanded;

			bool expanded;
			if (m_ExpandStates.TryGetValue(listData.index, out expanded))
				item.UpdateArrow(expanded, true);

			return item;
		}

		void ToggleExpanded(string index)
		{
			m_ExpandStates[index] = !m_ExpandStates[index];
			StartSettling();
		}

		public void SelectFolder(string guid)
		{
			if (data == null)
				return;

			selectedFolder = guid;

			if (data.Count >= 1)
			{
				var folderData = GetFolderDataByGUID(data[0], guid, fd =>
				{
					// Expand folders from root to selected folder
					if (fd.index != guid)
						m_ExpandStates[fd.index] = true;
				}) ?? data[0];
				selectFolder(folderData);
			}
		}

		static FolderData GetFolderDataByGUID(FolderData data, string guid, Action<FolderData> folderToRootCallback = null)
		{
			if (data.index == guid)
				return data;

			if (data.children != null)
			{
				foreach (var child in data.children)
				{
					var folder = GetFolderDataByGUID(child, guid);
					if (folder != null)
					{
						if (folderToRootCallback != null)
							folderToRootCallback(folder);

						return folder;
					}
				}
			}
			return null;
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_ExpandArrowMaterial);
		}
	}
}
#endif
