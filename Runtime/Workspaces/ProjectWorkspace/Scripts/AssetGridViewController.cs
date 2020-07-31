using System;
using System.Collections.Generic;
using Unity.EditorXR.Data;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Workspaces
{
    sealed class AssetGridViewController : EditorXRListViewController<AssetData, AssetGridItem, int>
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

        protected override void Awake()
        {
            base.Awake();

            m_OnRecycleComplete = OnRecycleComplete;
        }

        protected override void Start()
        {
            base.Start();

            m_ScrollOffset = itemSize.z * 0.5f;

            for (int i = 0; i < m_IconTypes.Length; i++)
            {
                if (!string.IsNullOrEmpty(m_IconTypes[i]) && m_Icons[i] != null)
                    m_IconDictionary[m_IconTypes[i]] = m_Icons[i];
            }
        }

        protected override void ComputeConditions()
        {
            m_ItemSize = GetObjectSize(m_Templates[0]);

            base.ComputeConditions();

            m_NumPerRow = (int)(m_Size.x / m_ItemSize.x);
            if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
                return;

            m_StartPosition = m_Extents.z * Vector3.forward + (m_Extents.x - m_ItemSize.x * 0.5f) * Vector3.left;

            // Snap back if list scrolled too far
            m_ScrollReturn = float.MaxValue;
            if (listHeight > 0 && -m_ScrollOffset >= listHeight)
            {
                m_ScrollReturn = -listHeight + m_ScaleFactor;

                if (m_Data.Count % m_NumPerRow == 0)
                    m_ScrollReturn += m_ItemSize.z;
            }
            // if we only have one row, snap back as soon as that row would be hidden
            else if (listHeight == m_ItemSize.z && -m_ScrollOffset > 0)
            {
                m_ScrollReturn = m_ItemSize.z / 2;
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
            m_TemplateDictionary[gridItem.data.template].pool.Enqueue(gridItem);
        }

        protected override AssetGridItem UpdateVisibleItem(AssetData datum, int order, float offset, ref bool doneSettling)
        {
            AssetGridItem item;
            if (!m_ListItems.TryGetValue(datum.index, out item))
                GetNewItem(datum, out item);

            if (item)
                UpdateGridItem(item, order, (int)offset);

            return item;
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

            var itemSize = m_ItemSize;
            var t = item.transform;
            var zOffset = itemSize.z * (count / m_NumPerRow) + m_ScrollOffset;
            var xOffset = itemSize.x * (count % m_NumPerRow);

            t.localPosition = Vector3.Lerp(t.localPosition, m_StartPosition + zOffset * Vector3.back + xOffset * Vector3.right, k_PositionFollow);
            t.localRotation = Quaternion.identity;

            if (t.GetSiblingIndex() != order)
                t.SetSiblingIndex(order);
        }

        protected override bool GetNewItem(AssetData data, out AssetGridItem item)
        {
            const float jitterMargin = 0.125f;
            if (Mathf.Abs(scrollOffset - m_LastHiddenItemOffset) < itemSize.z * jitterMargin) // Avoid jitter while scrolling rows in and out of view
            {
                item = null;
                return false;
            }

#if UNITY_EDITOR
            // If this AssetData hasn't fetched its asset yet, do so now
            if (data.asset == null)
            {
                data.asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid));
                data.preview = data.asset as GameObject;
            }
#endif

            var instantiated = base.GetNewItem(data, out item);

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

            return instantiated;
        }

        static void LoadFallbackTexture(AssetGridItem item, AssetData data)
        {
            item.fallbackTexture = null;
#if UNITY_EDITOR
            item.StartCoroutine(EditorUtils.GetAssetPreview(
                AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(data.guid)),
                texture => item.fallbackTexture = texture));
#endif
        }
    }
}
