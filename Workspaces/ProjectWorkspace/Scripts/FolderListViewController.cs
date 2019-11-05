using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class FolderListViewController : EditorXRNestedListViewController<FolderData, FolderListItem, int>
    {
        const float k_ClipMargin = 0.001f; // Give the cubes a margin so that their sides don't get clipped

        [SerializeField]
        Material m_TextMaterial;

        [SerializeField]
        Material m_ExpandArrowMaterial;

        int? m_SelectedFolder;

        public int selectedFolder
        {
            get { return m_SelectedFolder ?? 0; }
            set { SelectFolder(value); }
        }

        public Dictionary<int, bool> expandStates { get { return m_ExpandStates; } }

        public event Action<FolderData> folderSelected;

        public override List<FolderData> data
        {
            set
            {
                base.data = value;

                if (m_Data != null && m_Data.Count > 0)
                {
                    // Remove any folders that don't exist any more
                    var missingKeys = m_Data.Select(d => d.index).Except(m_ExpandStates.Keys);
                    foreach (var key in missingKeys)
                    {
                        m_ExpandStates.Remove(key);
                    }

                    foreach (var d in m_Data)
                    {
                        if (!m_ExpandStates.ContainsKey(d.index))
                            m_ExpandStates[d.index] = false;
                    }

                    // Expand and select the Assets folder by default
                    var guid = data[0].index;
                    m_ExpandStates[guid] = true;

                    SelectFolder(m_SelectedFolder ?? guid);
                }
            }
        }

        protected override void Start()
        {
            base.Start();

            m_TextMaterial = Instantiate(m_TextMaterial);
            m_ExpandArrowMaterial = Instantiate(m_ExpandArrowMaterial);
        }

        protected override void UpdateItems()
        {
            var parentMatrix = transform.worldToLocalMatrix;
            ClipText.SetMaterialClip(m_TextMaterial, parentMatrix, m_Extents);
            ClipText.SetMaterialClip(m_ExpandArrowMaterial, parentMatrix, m_Extents);

            base.UpdateItems();
        }

        void UpdateFolderItem(FolderData data, int order, float offset, int depth, bool expanded, ref bool doneSettling)
        {
            var index = data.index;
            FolderListItem item;
            if (!m_ListItems.TryGetValue(index, out item))
                GetNewItem(data, out item);

            item.UpdateSelf(m_Size.x - k_ClipMargin, depth, expanded, index == selectedFolder);

            ClipText.SetMaterialClip(item.cubeMaterial, transform.worldToLocalMatrix, m_Extents);

            UpdateItem(item, order, offset, ref doneSettling);
        }

        protected override void UpdateNestedItems(ref int order, ref float offset, ref bool doneSettling, int depth = 0)
        {
            m_UpdateStack.Push(new UpdateData
            {
                data = m_Data,
                depth = depth
            });

            while (m_UpdateStack.Count > 0)
            {
                var stackData = m_UpdateStack.Pop();
                var nestedData = stackData.data;
                depth = stackData.depth;

                var i = stackData.index;
                for (; i < nestedData.Count; i++)
                {
                    var datum = nestedData[i];
                    var index = datum.index;
                    bool expanded;
                    if (!m_ExpandStates.TryGetValue(index, out expanded))
                        m_ExpandStates[index] = false;

                    var localOffset = offset + m_ScrollOffset;
                    if (localOffset + itemSize.z < 0 || localOffset > m_Size.z)
                        Recycle(index);
                    else
                        UpdateFolderItem(datum, order++, localOffset, depth, expanded, ref doneSettling);

                    offset += itemSize.z;

                    if (datum.children != null)
                    {
                        if (expanded)
                        {
                            m_UpdateStack.Push(new UpdateData
                            {
                                data = nestedData,
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

        protected override bool GetNewItem(FolderData listData, out FolderListItem item)
        {
            var instantiated = base.GetNewItem(listData, out item);

            if (instantiated)
            {
                item.SetMaterials(m_TextMaterial, m_ExpandArrowMaterial);
                item.selectFolder = SelectFolder;
            }

            bool expanded;
            if (m_ExpandStates.TryGetValue(listData.index, out expanded))
                item.UpdateArrow(expanded, true);

            return instantiated;
        }

        protected override void ToggleExpanded(FolderData datum)
        {
            var index = datum.index;
            if (data.Count == 1 && m_ListItems[index].data == data[0]) // Do not collapse Assets folder
                return;

            base.ToggleExpanded(datum);
        }

        void SelectFolder(int guid)
        {
            m_SelectedFolder = guid;

            if (data == null)
                return;

            if (data.Count >= 1)
            {
                var folderData = GetFolderDataByGUID(data[0], guid) ?? data[0];

                if (folderSelected != null)
                    folderSelected(folderData);

                var scrollHeight = 0f;
                ScrollToIndex(data[0], guid, ref scrollHeight);
            }
        }

        static FolderData GetFolderDataByGUID(FolderData data, int guid)
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
