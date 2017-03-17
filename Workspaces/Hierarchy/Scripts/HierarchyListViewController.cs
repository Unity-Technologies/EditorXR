#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ListView;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class HierarchyListViewController : NestedListViewController<HierarchyData, HierarchyListItem, int>, IUsesGameObjectLocking
	{
		const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

		[SerializeField]
		BaseHandle m_TopDropZone;

		[SerializeField]
		BaseHandle m_BottomDropZone;

		[SerializeField]
		Material m_TextMaterial;

		[SerializeField]
		Material m_ExpandArrowMaterial;

		[SerializeField]
		Material m_LockIconMaterial;

		Material m_TopDropZoneMaterial;
		Material m_BottomDropZoneMaterial;
		float m_DropZoneAlpha;
		float m_BottomDropZoneStartHeight;
		float m_VisibleItemHeight;

		int m_SelectedRow;

		public Action<int> selectRow { private get; set; }

		public Func<string, bool> matchesFilter { private get; set; }
		public Func<string> getSearchQuery { private get; set; }

		public Action<GameObject, bool> setLocked { get; set; }
		public Func<GameObject, bool> isLocked { get; set; }

		protected override void Setup()
		{
			base.Setup();

			m_TextMaterial = Instantiate(m_TextMaterial);
			m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
			m_LockIconMaterial = Instantiate(m_LockIconMaterial);

			m_BottomDropZoneMaterial = MaterialUtils.GetMaterialClone(m_BottomDropZone.GetComponent<Renderer>());
			m_BottomDropZoneStartHeight = m_BottomDropZone.transform.localScale.z;
			m_TopDropZoneMaterial = MaterialUtils.GetMaterialClone(m_TopDropZone.GetComponent<Renderer>());
			var color = m_TopDropZoneMaterial.color;
			m_DropZoneAlpha = color.a;
			color.a = 0;
			m_TopDropZoneMaterial.color = color;
			m_BottomDropZoneMaterial.color = color;

			var dropZones = new[] { m_BottomDropZone, m_TopDropZone };
			foreach (var dropZone in dropZones)
			{
				dropZone.canDrop = CanDrop;
				dropZone.receiveDrop = RecieveDrop;
				dropZone.dropHoverStarted += DropHoverStarted;
				dropZone.dropHoverEnded += DropHoverEnded;
			}

			m_BottomDropZone.gameObject.SetActive(false); // Don't block scroll interaction
		}

		protected override void UpdateItems()
		{
			var parentMatrix = transform.worldToLocalMatrix;
			SetMaterialClip(m_TextMaterial, parentMatrix);
			SetMaterialClip(m_ExpandArrowMaterial, parentMatrix);
			SetMaterialClip(m_LockIconMaterial, parentMatrix);

			m_VisibleItemHeight = 0;

			base.UpdateItems();

			UpdateDropZones();
		}

		void UpdateDropZones()
		{
			var width = bounds.size.x - k_ClipMargin;
			var dropZoneTransform = m_TopDropZone.transform;
			var dropZoneScale = dropZoneTransform.localScale;
			dropZoneScale.x = width;
			dropZoneTransform.localScale = dropZoneScale;

			var dropZonePosition = dropZoneTransform.localPosition;
			dropZonePosition.z = bounds.extents.z + dropZoneScale.z * 0.5f;
			dropZoneTransform.localPosition = dropZonePosition;

			dropZoneTransform = m_BottomDropZone.transform;
			dropZoneScale = dropZoneTransform.localScale;
			dropZoneScale.x = width;
			var itemSize = m_ItemSize.Value.z;
			var extraSpace = bounds.size.z - m_VisibleItemHeight - scrollOffset % itemSize;
			dropZoneScale.z = extraSpace;

			dropZoneTransform.localScale = dropZoneScale;

			dropZonePosition = dropZoneTransform.localPosition;
			dropZonePosition.z = dropZoneScale.z * 0.5f - bounds.extents.z;
			dropZoneTransform.localPosition = dropZonePosition;

			if (extraSpace < m_BottomDropZoneStartHeight)
			{
				dropZoneScale.z = m_BottomDropZoneStartHeight;
				dropZoneTransform.localScale = dropZoneScale;
				dropZonePosition.z = -dropZoneScale.z * 0.5f - bounds.extents.z;
			}
		}

		void UpdateHierarchyItem(HierarchyData data, ref float offset, int depth, bool? expanded, ref bool doneSettling)
		{
			var index = data.index;
			HierarchyListItem item;
			if (!m_ListItems.TryGetValue(index, out item))
				item = GetItem(data);

			var width = bounds.size.x - k_ClipMargin;
			item.UpdateSelf(width, depth, expanded, index == m_SelectedRow, data.locked);

			SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix);
			SetMaterialClip(item.dropZoneMaterial, transform.worldToLocalMatrix);

			m_VisibleItemHeight+= itemSize.z;
			UpdateItem(item.transform, offset + m_ScrollOffset, ref doneSettling);

			var extraSpace = item.extraSpace * itemSize.z;
			offset += extraSpace;
			m_VisibleItemHeight += extraSpace;
		}

		protected override void UpdateRecursively(List<HierarchyData> data, ref float offset, ref bool doneSettling, int depth = 0)
		{
			for (int i = 0; i < data.Count; i++)
			{
				var datum = data[i];
				var index = datum.index;
				bool expanded;
				m_ExpandStates.TryGetValue(index, out expanded);

				var grabbed = m_GrabbedRows.ContainsKey(index);

				if (grabbed)
				{
					var item = GetListItem(index);
					if (item && item.isStillSettling) // "Hang on" to settle state until grabbed object is settled in the list
						doneSettling = false;
					continue;
				}

				var hasChildren = datum.children != null;

				var hasFilterQuery = !string.IsNullOrEmpty(getSearchQuery());
				var shouldRecycle = offset + scrollOffset + itemSize.z < 0 || offset + scrollOffset > bounds.size.z;
				if (hasFilterQuery)
				{
					var filterTestPass = datum.types.Any(type => matchesFilter(type));

					if (!filterTestPass) // If this item doesn't match the filter, move on to the next item; do not count
					{
						Recycle(index);
					}
					else
					{
						if (shouldRecycle)
							Recycle(index);
						else
							UpdateHierarchyItem(datum, ref offset, 0, null, ref doneSettling);

						offset += itemSize.z;
					}

					if (hasChildren)
						UpdateRecursively(datum.children, ref offset, ref doneSettling);
				}
				else
				{
					if (shouldRecycle)
						Recycle(index);
					else
						UpdateHierarchyItem(datum, ref offset, depth, expanded, ref doneSettling);

					offset += itemSize.z;

					if (hasChildren)
					{
						if (expanded)
							UpdateRecursively(datum.children, ref offset, ref doneSettling, depth + 1);
						else
							RecycleChildren(datum);
					}
					else
					{
						m_ExpandStates[index] = false;
					}
				}
			}
		}

		protected override HierarchyListItem GetItem(HierarchyData data)
		{
			var item = base.GetItem(data);
			item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial, m_LockIconMaterial);
			item.selectRow = SelectRow;

			item.setRowGrabbed = SetRowGrabbed;
			item.getGrabbedRow = GetGrabbedRow;

			item.toggleLock = ToggleLock;

			item.toggleExpanded = ToggleExpanded;
			item.setExpanded = SetExpanded;
			item.isExpanded = GetExpanded;

			item.UpdateArrow(GetExpanded(data.index), true);

			return item;
		}

		protected override void SetRowGrabbed(int index, Transform rayOrigin, bool grabbed)
		{
			base.SetRowGrabbed(index, rayOrigin, grabbed);
			m_BottomDropZone.gameObject.SetActive(m_GrabbedRows.Count > 0); // Don't block scroll interaction
		}

		void ToggleLock(int index)
		{
			HierarchyListItem listItem;
			if (m_ListItems.TryGetValue(index, out listItem))
			{
				var data = listItem.data;
				var go = EditorUtility.InstanceIDToObject(data.index) as GameObject;
				if (go)
					setLocked(go, !isLocked(go));
			}
		}

		void ToggleExpanded(int index)
		{
			bool expanded;
			if (!m_ExpandStates.TryGetValue(index, out expanded))
				m_ExpandStates[index] = true;
			else
				m_ExpandStates[index] = !expanded;

			StartSettling();
		}

		public void SelectRow(int index)
		{
			if (data == null)
				return;

			m_SelectedRow = index;

			foreach (var datum in data)
			{
				ExpandToRow(datum, index);
			}

			selectRow(index);

			var scrollHeight = 0f;
			foreach (var datum in data)
			{
				ScrollToRow(datum, index, ref scrollHeight);
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

		static bool CanDrop(BaseHandle handle, object dropObject)
		{
			return dropObject is HierarchyData;
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
					gameObject.transform.SetAsFirstSibling();
				}
			}

			if (handle == m_BottomDropZone)
			{
				var hierarchyData = dropObject as HierarchyData;
				if (hierarchyData != null)
				{
					var gameObject = EditorUtility.InstanceIDToObject(hierarchyData.index) as GameObject;
					gameObject.transform.SetParent(null);
					gameObject.transform.SetAsLastSibling();
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

		bool GetExpanded(int index)
		{
			bool expanded;
			m_ExpandStates.TryGetValue(index, out expanded);
			return expanded;
		}

		void SetExpanded(int index, bool expanded)
		{
			m_ExpandStates[index] = expanded;
			StartSettling();
		}

		public void OnScroll(float delta)
		{
			if (m_Settling)
				return;

			scrollOffset += delta;
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_ExpandArrowMaterial);
			ObjectUtils.Destroy(m_LockIconMaterial);
		}
	}
}
#endif
