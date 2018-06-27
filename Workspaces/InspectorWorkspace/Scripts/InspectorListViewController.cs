#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ListView;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorListViewController : NestedListViewController<InspectorData, InspectorListItem, int>, IUsesGameObjectLocking, IUsesStencilRef
    {
        const string k_MaterialStencilRef = "_StencilRef";
        const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

        [SerializeField]
        Material m_RowCubeMaterial;

        [SerializeField]
        Material m_BackingCubeMaterial;

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

        public event Action<List<InspectorData>, PropertyData> arraySizeChanged;

        protected override void Setup()
        {
            base.Setup();

            m_RowCubeMaterial = Instantiate(m_RowCubeMaterial);
            m_BackingCubeMaterial = Instantiate(m_BackingCubeMaterial);
            m_UIMaterial = Instantiate(m_UIMaterial);
            m_UIMaterial.SetInt(k_MaterialStencilRef, stencilRef);
            m_UIMaskMaterial = Instantiate(m_UIMaskMaterial);
            m_UIMaskMaterial.SetInt(k_MaterialStencilRef, stencilRef);

            m_HighlightMaterial = Instantiate(m_HighlightMaterial);
            m_HighlightMaterial.SetInt(k_MaterialStencilRef, stencilRef);
            m_HighlightMaskMaterial = Instantiate(m_HighlightMaskMaterial);
            m_HighlightMaskMaterial.SetInt(k_MaterialStencilRef, stencilRef);

            m_NoClipBackingCubeMaterial = Instantiate(m_NoClipBackingCubeMaterial);
            m_NoClipHighlightMaterial = Instantiate(m_NoClipHighlightMaterial);
            m_NoClipHighlightMaskMaterial = Instantiate(m_NoClipHighlightMaskMaterial);

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

            m_StartPosition = m_Extents.z * Vector3.back;

            var parentMatrix = transform.worldToLocalMatrix;
            SetMaterialClip(m_RowCubeMaterial, parentMatrix);
            SetMaterialClip(m_BackingCubeMaterial, parentMatrix);
            SetMaterialClip(m_UIMaterial, parentMatrix);
            SetMaterialClip(m_UIMaskMaterial, parentMatrix);
            SetMaterialClip(m_HighlightMaterial, parentMatrix);
            SetMaterialClip(m_HighlightMaskMaterial, parentMatrix);
        }

        public void OnObjectModified()
        {
            foreach (var item in m_ListItems.Values)
            {
                item.OnObjectModified();
            }
        }

        protected override void UpdateNestedItems(List<InspectorData> data, ref int order, ref float offset, ref bool doneSettling, int depth = 0)
        {
            m_UpdateStack.Push(new UpdateData
            {
                data = data,
                depth = depth
            });

            order = m_ListItems.Count - 1;
            while (m_UpdateStack.Count > 0)
            {
                var stackData = m_UpdateStack.Pop();
                data = stackData.data;
                depth = stackData.depth;

                var i = stackData.index;
                for (; i < data.Count; i++)
                {
                    var datum = data[i];
                    var serializedObject = datum.serializedObject;
                    if (serializedObject == null || serializedObject.targetObject == null)
                    {
                        Recycle(datum.index);
                        RecycleChildren(datum);
                        continue;
                    }

                    var index = datum.index;
                    bool expanded;
                    if (!m_ExpandStates.TryGetValue(index, out expanded))
                        m_ExpandStates[index] = false;

                    m_ItemSize = m_TemplateSizes[datum.template];
                    var itemSize = m_ItemSize.Value;

                    if (offset + scrollOffset + itemSize.z < 0 || offset + scrollOffset > m_Size.z)
                        Recycle(index);
                    else
                        UpdateInspectorItem(datum, order--, offset, depth, expanded, ref doneSettling);

                    offset += itemSize.z;

                    if (datum.children != null)
                    {
                        if (expanded)
                        {
                            m_UpdateStack.Push(new UpdateData
                            {
                                data = data,
                                depth = depth,

                                index = i + 1
                            });

                            m_UpdateStack.Push(new UpdateData
                            {
                                data = datum.children,
                                depth = depth + 1
                            });
                            break;
                        }

                        RecycleChildren(datum);
                    }
                }
            }
        }

        void UpdateInspectorItem(InspectorData data, int order, float offset, int depth, bool expanded, ref bool doneSettling)
        {
            InspectorListItem item;
            if (!m_ListItems.TryGetValue(data.index, out item))
            {
                item = GetItem(data);
                UpdateItem(item.transform, order, offset, true, ref doneSettling);
            }

            item.UpdateSelf(m_Size.x - k_ClipMargin, depth, expanded);
            item.UpdateClipTexts(transform.worldToLocalMatrix, m_Extents);

            UpdateItem(item.transform, order, offset, false, ref doneSettling);
        }

        void UpdateItem(Transform t, int order, float offset, bool dontSettle, ref bool doneSettling)
        {
            var targetPosition = m_StartPosition + (offset + m_ScrollOffset) * Vector3.forward;
            var targetRotation = Quaternion.identity;

            UpdateItemTransform(t, order, targetPosition, targetRotation, dontSettle, ref doneSettling);
        }

        protected override InspectorListItem GetItem(InspectorData listData)
        {
            var item = base.GetItem(listData);

            item.setRowGrabbed = SetRowGrabbed;
            item.getGrabbedRow = GetGrabbedRow;
            item.toggleExpanded = ToggleExpanded;

            if (!item.setup)
            {
                var highlightMaterials = new[] { m_HighlightMaterial, m_HighlightMaskMaterial };
                var noClipHighlightMaterials = new[] { m_NoClipHighlightMaterial, m_NoClipHighlightMaskMaterial };
                item.SetMaterials(m_RowCubeMaterial, m_BackingCubeMaterial, m_UIMaterial, m_UIMaskMaterial, m_NoClipBackingCubeMaterial, highlightMaterials, noClipHighlightMaterials);

                var numberItem = item as InspectorNumberItem;
                if (numberItem)
                    numberItem.arraySizeChanged += OnArraySizeChanged;

                item.setup = true;
            }

            var headerItem = item as InspectorHeaderItem;
            if (headerItem)
            {
                var go = (GameObject)listData.serializedObject.targetObject;
                headerItem.setLocked = locked => this.SetLocked(go, locked);
                headerItem.lockToggle.isOn = this.IsLocked(go);
            }

            return item;
        }

        public void OnBeforeChildrenChanged(ListViewItemNestedData<InspectorData, int> data, List<InspectorData> newData)
        {
            InspectorNumberItem arraySizeItem = null;
            var children = data.children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    var index = child.index;
                    InspectorListItem item;
                    if (m_ListItems.TryGetValue(index, out item))
                    {
                        var childNumberItem = item as InspectorNumberItem;
                        if (childNumberItem && childNumberItem.propertyType == SerializedPropertyType.ArraySize)
                            arraySizeItem = childNumberItem;
                        else
                            Recycle(index);
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
                        m_ListItems[propChild.index] = arraySizeItem;
                        arraySizeItem.data = propChild;
                    }
                }
            }
        }

        void ToggleExpanded(int index)
        {
            m_ExpandStates[index] = !m_ExpandStates[index];
            StartSettling();
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
                var targetObject = datum.serializedObject.targetObject;
                m_ExpandStates[datum.index] = targetObject is Component || targetObject is GameObject;

                if (datum.children != null)
                    ExpandComponentRows(datum.children);
            }
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_RowCubeMaterial);
            ObjectUtils.Destroy(m_BackingCubeMaterial);
            ObjectUtils.Destroy(m_UIMaterial);
            ObjectUtils.Destroy(m_UIMaskMaterial);
            ObjectUtils.Destroy(m_HighlightMaterial);
            ObjectUtils.Destroy(m_HighlightMaskMaterial);
            ObjectUtils.Destroy(m_NoClipBackingCubeMaterial);
            ObjectUtils.Destroy(m_NoClipHighlightMaterial);
            ObjectUtils.Destroy(m_NoClipHighlightMaskMaterial);
        }
    }
}
#endif
