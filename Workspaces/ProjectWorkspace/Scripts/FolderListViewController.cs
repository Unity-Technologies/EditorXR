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
					var guid = data[0].index;
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

		void UpdateFolderItem(FolderData data, float offset, int depth, bool expanded, ref bool doneSettling)
		{
			var index = data.index;
			FolderListItem item;
			if (!m_ListItems.TryGetValue(index, out item))
				item = GetItem(data);

			item.UpdateSelf(bounds.size.x - k_ClipMargin, depth, expanded, index == m_SelectedFolder);

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
			if (data.index == guid)
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

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_ExpandArrowMaterial);
		}
	}
}
#endif
