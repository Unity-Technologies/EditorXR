#if UNITY_EDITOR
using ListView;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
	sealed class InspectorListViewController : NestedListViewController<InspectorData>, IGetPreviewOrigin, ISetHighlight, IUsesGameObjectLocking, IUsesStencilRef
	{
		const string k_MaterialStencilRef = "_StencilRef";
		const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

		[SerializeField]
		Material m_RowCubeMaterial;

		[SerializeField]
		Material m_BackingCubeMaterial;

		[SerializeField]
		Material m_TextMaterial;

		[SerializeField]
		Material m_UIMaterial;

		[SerializeField]
		Material m_UIMaskMaterial;
		[SerializeField]
		Material m_NoClipBackingCubeMaterial;

		[SerializeField]
		Material m_HighlightMaterial;

		[SerializeField]
		Material m_HighlightMaskMaterial;

		[SerializeField]
		Material m_NoClipHighlightMaterial;

		[SerializeField]
		Material m_NoClipHighlightMaskMaterial;

		readonly Dictionary<string, Vector3> m_TemplateSizes = new Dictionary<string, Vector3>();

		readonly Dictionary<int, bool> m_ExpandStates = new Dictionary<int, bool>();

		public override List<InspectorData> data
		{
			set
			{
				base.data = value;
				m_ExpandStates.Clear();

				ExpandComponentRows(data);
			}
		}

		public byte stencilRef { get; set; }

		public Action<GameObject, bool> setHighlight { private get; set; }

		public Func<Transform, Transform> getPreviewOriginForRayOrigin { private get; set; }

		public Action<GameObject, bool> setLocked { private get; set; }
		public Func<GameObject, bool> isLocked { private get; set; }

		public event Action<List<InspectorData>, PropertyData> arraySizeChanged;

		protected override void Setup()
		{
			base.Setup();

			m_RowCubeMaterial = Instantiate(m_RowCubeMaterial);
			m_BackingCubeMaterial = Instantiate(m_BackingCubeMaterial);
			m_NoClipBackingCubeMaterial = Instantiate(m_NoClipBackingCubeMaterial);
			m_TextMaterial = Instantiate(m_TextMaterial);
			m_TextMaterial.SetInt(k_MaterialStencilRef, stencilRef);
			m_UIMaterial = Instantiate(m_UIMaterial);
			m_UIMaterial.SetInt(k_MaterialStencilRef, stencilRef);
			m_UIMaskMaterial = Instantiate(m_UIMaskMaterial);
			m_UIMaskMaterial.SetInt(k_MaterialStencilRef, stencilRef);

			m_HighlightMaterial = Instantiate(m_HighlightMaterial);
			m_HighlightMaterial.SetInt(k_MaterialStencilRef, stencilRef);
			m_HighlightMaskMaterial = Instantiate(m_HighlightMaskMaterial);
			m_HighlightMaskMaterial.SetInt(k_MaterialStencilRef, stencilRef);

			m_NoClipHighlightMaterial = Instantiate(m_NoClipHighlightMaterial);
			m_NoClipHighlightMaterial.SetInt(k_MaterialStencilRef, 0);
			m_NoClipHighlightMaskMaterial = Instantiate(m_NoClipHighlightMaskMaterial);
			m_NoClipHighlightMaskMaterial.SetInt(k_MaterialStencilRef, 0);

			m_NoClipHighlightMaterial = Instantiate(m_NoClipHighlightMaterial);
			m_NoClipHighlightMaterial.SetInt(k_MaterialStencilRef, 0);
			m_NoClipHighlightMaskMaterial = Instantiate(m_NoClipHighlightMaskMaterial);
			m_NoClipHighlightMaskMaterial.SetInt(k_MaterialStencilRef, 0);

			foreach (var template in m_TemplateDictionary)
				m_TemplateSizes[template.Key] = GetObjectSize(template.Value.prefab);

			if (data == null)
				data = new List<InspectorData>();
		}

		protected override void ComputeConditions()
		{
			// Check if object was deleted
			if (data.Count > 0 && !data[0].serializedObject.targetObject)
				data = new List<InspectorData>();

			base.ComputeConditions();

			m_StartPosition = bounds.extents.z * Vector3.back;

			var parentMatrix = transform.worldToLocalMatrix;
			SetMaterialClip(m_RowCubeMaterial, parentMatrix);
			SetMaterialClip(m_BackingCubeMaterial, parentMatrix);
			SetMaterialClip(m_TextMaterial, parentMatrix);
			SetMaterialClip(m_UIMaterial, parentMatrix);
			SetMaterialClip(m_UIMaskMaterial, parentMatrix);
			SetMaterialClip(m_HighlightMaterial, parentMatrix);
			SetMaterialClip(m_HighlightMaskMaterial, parentMatrix);
		}

		protected override void UpdateItems()
		{
			var totalOffset = 0f;
			UpdateRecursively(m_Data, ref totalOffset);

			// Snap back if list scrolled too far
			if (totalOffset > 0 && -scrollOffset >= totalOffset)
				m_ScrollReturn = -totalOffset + m_ItemSize.Value.z; // m_ItemSize will be equal to the size of the last visible item
		}

		public void OnObjectModified()
		{
			foreach (var listViewItem in m_ListItems.Values)
			{
				var item = (InspectorListItem)listViewItem;
				item.OnObjectModified();
			}
		}

		void UpdateRecursively(List<InspectorData> data, ref float totalOffset, int depth = 0)
		{
			foreach (var datum in data)
			{
				if (datum.instanceID == null)
				{
					Recycle(datum);
					RecycleChildren(datum);
					continue;
				}

				bool expanded;
				var instanceID = datum.instanceID.Value;
				if (!m_ExpandStates.TryGetValue(instanceID, out expanded))
					m_ExpandStates[instanceID] = false;

				m_ItemSize = m_TemplateSizes[datum.template];
				var itemSize = m_ItemSize.Value;

				if (totalOffset + scrollOffset + itemSize.z < 0 || totalOffset + scrollOffset > bounds.size.z)
					Recycle(datum);
				else
					UpdateItemRecursive(datum, totalOffset, depth, expanded);

				totalOffset += itemSize.z;

				if (datum.children != null)
				{
					if (expanded)
						UpdateRecursively(datum.children, ref totalOffset, depth + 1);
					else
						RecycleChildren(datum);
				}
			}
		}

		void UpdateItemRecursive(InspectorData data, float offset, int depth, bool expanded)
		{
			ListViewItem<InspectorData> item;
			if (!m_ListItems.TryGetValue(data, out item))
				item = GetItem(data);

			var inspectorListItem = (InspectorListItem)item;
			inspectorListItem.UpdateSelf(bounds.size.x - k_ClipMargin, depth, expanded);
			inspectorListItem.UpdateClipTexts(transform.worldToLocalMatrix, bounds.extents);

			UpdateItem(item.transform, offset);
		}

		void UpdateItem(Transform t, float offset)
		{
			t.localPosition = m_StartPosition + (offset + m_ScrollOffset) * Vector3.forward;
			t.localRotation = Quaternion.identity;
		}

		protected override void RecycleItem(string template, MonoBehaviour item)
		{
			var headerItem = item as InspectorHeaderItem;
			if (headerItem)
				headerItem.setLocked = null; // Reset, so it doesn't get called for the wrong object when re-used

			base.RecycleItem(template, item);
		}

		protected override ListViewItem<InspectorData> GetItem(InspectorData listData)
		{
			var item = (InspectorListItem)base.GetItem(listData);

			if (!item.setup)
			{
				var highlightMaterials = new[] { m_HighlightMaterial, m_HighlightMaskMaterial };
				var noClipHighlightMaterials = new[] { m_NoClipHighlightMaterial, m_NoClipHighlightMaskMaterial };
				item.SetMaterials(m_RowCubeMaterial, m_BackingCubeMaterial, m_UIMaterial, m_UIMaskMaterial, m_TextMaterial, m_NoClipBackingCubeMaterial, highlightMaterials, noClipHighlightMaterials);

				item.setHighlight = setHighlight;
				item.getPreviewOriginForRayOrigin = getPreviewOriginForRayOrigin;

				var numberItem = item as InspectorNumberItem;
				if (numberItem)
					numberItem.arraySizeChanged += OnArraySizeChanged;

				item.setup = true;
			}

			var headerItem = item as InspectorHeaderItem;
			if (headerItem)
			{
				var go = (GameObject)listData.serializedObject.targetObject;
				headerItem.setLocked = locked => setLocked(go, locked);
				headerItem.lockToggle.isOn = isLocked(go);
			}

			item.toggleExpanded = ToggleExpanded;

			return item;
		}

		public void OnBeforeChildrenChanged(ListViewItemNestedData<InspectorData> data, List<InspectorData> newData)
		{
			InspectorNumberItem arraySizeItem = null;
			var children = data.children;
			if (children != null)
			{
				foreach (var child in children)
				{
					ListViewItem<InspectorData> item;
					if (m_ListItems.TryGetValue(child, out item))
					{
						var childNumberItem = item as InspectorNumberItem;
						if (childNumberItem && childNumberItem.propertyType == SerializedPropertyType.ArraySize)
							arraySizeItem = childNumberItem;
						else
							Recycle(child);
					}
				}
			}

			// Re-use InspectorNumberItem for array Size in case we are dragging the value
			if (arraySizeItem)
			{
				foreach (var child in newData)
				{
					var propChild = child as PropertyData;
					if (propChild != null && propChild.property.propertyType == SerializedPropertyType.ArraySize)
					{
						m_ListItems[propChild] = arraySizeItem;
						arraySizeItem.data = propChild;
					}
				}
			}
		}

		void ToggleExpanded(InspectorData data)
		{
			var instanceID = data.instanceID.Value;
			m_ExpandStates[instanceID] = !m_ExpandStates[instanceID];
		}

		void OnArraySizeChanged(PropertyData element)
		{
			if (arraySizeChanged != null)
				arraySizeChanged(m_Data, element);
		}

		void ExpandComponentRows(List<InspectorData> data)
		{
			foreach (var datum in data)
			{
				if (datum.instanceID == null)
					continue;

				var targetObject = datum.serializedObject.targetObject;
				m_ExpandStates[datum.instanceID.Value] = targetObject is Component || targetObject is GameObject;

				if (datum.children != null)
					ExpandComponentRows(datum.children);
			}
		}

		void OnDestroy()
		{
			ObjectUtils.Destroy(m_RowCubeMaterial);
			ObjectUtils.Destroy(m_BackingCubeMaterial);
			ObjectUtils.Destroy(m_NoClipBackingCubeMaterial);
			ObjectUtils.Destroy(m_TextMaterial);
			ObjectUtils.Destroy(m_UIMaterial);
			ObjectUtils.Destroy(m_UIMaskMaterial);
			ObjectUtils.Destroy(m_HighlightMaterial);
			ObjectUtils.Destroy(m_HighlightMaskMaterial);
			ObjectUtils.Destroy(m_NoClipHighlightMaterial);
			ObjectUtils.Destroy(m_NoClipHighlightMaskMaterial);
		}
	}
}
#endif
