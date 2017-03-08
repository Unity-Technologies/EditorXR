#if UNITY_EDITOR
using ListView;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class FolderListViewController : NestedListViewController<FolderData, string>
	{
		private const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

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

		void UpdateFolderItem(FolderData data, int offset, int depth, bool expanded)
		{
			var index = data.index;
			ListViewItem<FolderData, string> item;
			if (!m_ListItems.TryGetValue(index, out item))
				item = GetItem(data);

			var folderItem = (FolderListItem)item;

			folderItem.UpdateSelf(bounds.size.x - k_ClipMargin, depth, expanded, index == m_SelectedFolder);

			SetMaterialClip(folderItem.cubeMaterial, transform.worldToLocalMatrix);

			UpdateItemTransform(item.transform, offset);
		}

		protected override void UpdateRecursively(List<FolderData> data, ref int count, int depth = 0)
		{
			foreach (var datum in data)
			{
				var index = datum.index;
				bool expanded;
				if (!m_ExpandStates.TryGetValue(index, out expanded))
					m_ExpandStates[index] = false;

				if (count + m_DataOffset < -1 || count + m_DataOffset > m_NumRows - 1)
					Recycle(index);
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

		protected override ListViewItem<FolderData, string> GetItem(FolderData listData)
		{
			var item = (FolderListItem)base.GetItem(listData);
			item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
			item.selectFolder = SelectFolder;

			item.toggleExpanded = ToggleExpanded;

			bool expanded;
			if (m_ExpandStates.TryGetValue(listData.index, out expanded))
				item.UpdateArrow(expanded, true);

			return item;
		}

		void ToggleExpanded(FolderData data)
		{
			var index = data.index;
			m_ExpandStates[index] = !m_ExpandStates[index];
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

		private void OnDestroy()
		{
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_ExpandArrowMaterial);
		}
	}
}
#endif
