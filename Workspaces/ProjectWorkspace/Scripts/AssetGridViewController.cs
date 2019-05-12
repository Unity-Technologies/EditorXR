using ListView;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class AssetGridViewController : ListViewController<AssetData, AssetGridItem, int>
    {
        const float k_PositionFollow = 0.4f;

#pragma warning disable 649
        [SerializeField]
        float m_ScaleFactor = 0.05f;

        [SerializeField]
        string[] m_IconTypes;

        [SerializeField]
        GameObject[] m_Icons;
#pragma warning restore 649

        Transform m_GrabbedObject;

        int m_NumPerRow;

        float m_LastHiddenItemOffset;

        readonly Dictionary<string, GameObject> m_IconDictionary = new Dictionary<string, GameObject>();

        Action<AssetGridItem> m_OnRecycleComplete;

        public float scaleFactor
        {
            get { return m_ScaleFactor; }
            set
            {
                m_LastHiddenItemOffset = Mathf.Infinity; // Allow any change in scale to change visibility states
                m_ScaleFactor = value;
            }
        }

        public Func<string, bool> matchesFilter { private get; set; }

        protected override float listHeight
        {
            get
            {
                if (m_NumPerRow == 0)
                    return 0;

                var numRows = Mathf.CeilToInt(m_Data.Count / m_NumPerRow);
                return Mathf.Clamp(numRows, 1, int.MaxValue) * itemSize.z;
            }
        }

        public override List<AssetData> data
        {
            set
            {
                base.data = value;

                m_LastHiddenItemOffset = Mathf.Infinity;
            }
        }

        public override Vector3 size
        {
            set
            {
                base.size = value;
                m_LastHiddenItemOffset = Mathf.Infinity;
            }
        }

        void Awake()
        {
            m_OnRecycleComplete = OnRecycleComplete;
        }

        protected override void Setup()
        {
            base.Setup();

            m_ScrollOffset = itemSize.z * 0.5f;

            for (int i = 0; i < m_IconTypes.Length; i++)
            {
                if (!string.IsNullOrEmpty(m_IconTypes[i]) && m_Icons[i] != null)
                    m_IconDictionary[m_IconTypes[i]] = m_Icons[i];
            }
        }

        protected override void ComputeConditions()
        {
            base.ComputeConditions();

            var itemSize = m_ItemSize.Value;
            m_NumPerRow = (int)(m_Size.x / itemSize.x);
            if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
                return;

            m_StartPosition = m_Extents.z * Vector3.forward + (m_Extents.x - itemSize.x * 0.5f) * Vector3.left;

            // Snap back if list scrolled too far
            m_ScrollReturn = float.MaxValue;
            if (listHeight > 0 && -m_ScrollOffset >= listHeight)
            {
                m_ScrollReturn = -listHeight + m_ScaleFactor;

                if (m_Data.Count % m_NumPerRow == 0)
                    m_ScrollReturn += itemSize.z;
            }
            // if we only have one row, snap back as soon as that row would be hidden
            else if (listHeight == itemSize.z && -m_ScrollOffset > 0)
            {
                m_ScrollReturn = itemSize.z / 2;
            }
        }

        protected override Vector3 GetObjectSize(GameObject g)
        {
            return g.GetComponent<BoxCollider>().size * m_ScaleFactor + Vector3.one * m_Padding * m_ScaleFactor;
        }

        protected override void UpdateItems()
        {
            var count = 0;
            var order = 0;
            foreach (var data in m_Data)
            {
                if (m_NumPerRow == 0) // If the list is too narrow, display nothing
                {
                    RecycleGridItem(data);
                    continue;
                }

                if (!matchesFilter(data.type)) // If this item doesn't match the filter, move on to the next item; do not count
                {
                    RecycleGridItem(data);
                    continue;
                }

                var offset = count / m_NumPerRow * itemSize.z;
                if (offset + scrollOffset < 0 || offset + scrollOffset > m_Size.z)
                    RecycleGridItem(data);
                else
                {
                    var ignored = true;
                    UpdateVisibleItem(data, order++, count, ref ignored);
                }

                count++;
            }
        }

        void RecycleGridItem(AssetData data)
        {
            var index = data.index;
            AssetGridItem item;
            if (!m_ListItems.TryGetValue(index, out item))
                return;

            m_LastHiddenItemOffset = scrollOffset;

            m_ListItems.Remove(index);

            item.SetVisibility(false, m_OnRecycleComplete);
        }

        void OnRecycleComplete(AssetGridItem gridItem)
        {
            gridItem.gameObject.SetActive(false);
            m_TemplateDictionary[gridItem.data.template].pool.Add(gridItem);
        }

        protected override void UpdateVisibleItem(AssetData data, int order, float offset, ref bool doneSettling)
        {
            AssetGridItem item;
            if (!m_ListItems.TryGetValue(data.index, out item))
                item = GetItem(data);

            if (item)
                UpdateGridItem(item, order, (int)offset);
        }

        public override void OnScrollEnded()
        {
            m_Scrolling = false;
            if (m_ScrollOffset > m_ScaleFactor)
            {
                m_ScrollOffset = m_ScaleFactor;
                m_ScrollDelta = 0;
            }
            if (m_ScrollReturn < float.MaxValue)
            {
                m_ScrollOffset = m_ScrollReturn;
                m_ScrollReturn = float.MaxValue;
                m_ScrollDelta = 0;
            }
        }

        void UpdateGridItem(AssetGridItem item, int order, int count)
        {
            item.UpdateTransforms(m_ScaleFactor);

            var itemSize = m_ItemSize.Value;
            var t = item.transform;
            var zOffset = itemSize.z * (count / m_NumPerRow) + m_ScrollOffset;
            var xOffset = itemSize.x * (count % m_NumPerRow);

            t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, k_PositionFollow);
            t.localRotation = Quaternion.identity;

            if (t.GetSiblingIndex() != order)
                t.SetSiblingIndex(order);
        }

        protected override AssetGridItem GetItem(AssetData data)
        {
            const float jitterMargin = 0.125f;
            if (Mathf.Abs(scrollOffset - m_LastHiddenItemOffset) < itemSize.z * jitterMargin) // Avoid jitter while scrolling rows in and out of view
                return null;

#if UNITY_EDITOR
            // If this AssetData hasn't fetched its asset yet, do so now
            if (data.asset == null)
            {
                data.asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid));
                data.preview = data.asset as GameObject;
            }
#endif

            var item = base.GetItem(data);

            item.transform.localPosition = m_StartPosition;

            item.scaleFactor = m_ScaleFactor;
            item.SetVisibility(true);

            switch (data.type)
            {
                case "Material":
                    var material = data.asset as Material;
                    if (material)
                        item.material = material;
                    else
                        LoadFallbackTexture(item, data);
                    break;
                case "Texture2D":
                    goto case "Texture";
                case "Texture":
                    var texture = data.asset as Texture;
                    if (texture)
                        item.texture = texture;
                    else
                        LoadFallbackTexture(item, data);
                    break;
                default:
                    GameObject icon;
                    if (m_IconDictionary.TryGetValue(data.type, out icon))
                        item.icon = icon;
                    else
                        LoadFallbackTexture(item, data);
                    break;
            }
            return item;
        }

        static void LoadFallbackTexture(AssetGridItem item, AssetData data)
        {
            item.fallbackTexture = null;
#if UNITY_EDITOR
            item.StartCoroutine(ObjectUtils.GetAssetPreview(
                AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid)),
                texture => item.fallbackTexture = texture));
#endif
        }
    }
}
