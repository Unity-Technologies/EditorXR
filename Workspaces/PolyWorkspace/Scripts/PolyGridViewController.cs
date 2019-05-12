using ListView;
using UnityEngine;

#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
#endif

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    class PolyGridViewController : ListViewController<PolyGridAsset, PolyGridItem, string>, IPoly
    {
        const int k_RequestSize = 100;

        const float k_PositionFollow = 0.4f;

#pragma warning disable 649
        [SerializeField]
        float m_ScaleFactor = 0.05f;

        [SerializeField]
        Transform m_Spinner;

        [SerializeField]
        float m_SpinnerSpeed = 25f;

        [SerializeField]
        Vector3 m_SpinnerOffset = new Vector3(0.08f, -0.0125f, -0.023f);

        [SerializeField]
        string[] m_IconTypes;

        [SerializeField]
        GameObject[] m_Icons;
#pragma warning restore 649

        Transform m_GrabbedObject;

        int m_NumPerRow;

        float m_LastHiddenItemOffset;
        int m_LastDataCount;
        string m_NextPageToken;

#if INCLUDE_POLY_TOOLKIT
        PolyOrderBy m_Sorting;
        PolyMaxComplexityFilter m_Complexity;
        PolyFormatFilter? m_Format;
        PolyCategory m_Category;

        public PolyOrderBy sorting { private get; set; }
        public PolyMaxComplexityFilter complexity { private get; set; }
        public PolyFormatFilter? format { private get; set; }
        public PolyCategory category { private get; set; }
#endif

        public float scaleFactor
        {
            get { return m_ScaleFactor; }
            set
            {
                m_LastHiddenItemOffset = Mathf.Infinity; // Allow any change in scale to change visibility states
                m_ScaleFactor = value;
            }
        }

        protected override float listHeight
        {
            get
            {
                if (m_NumPerRow == 0)
                    return 0;

                return Mathf.CeilToInt(m_Data.Count / m_NumPerRow) * itemSize.z;
            }
        }

        protected override void Setup()
        {
            base.Setup();

            m_ScrollOffset = itemSize.z * 0.5f;

            RequestAssetList();
        }

        public void RequestAssetList()
        {
#if INCLUDE_POLY_TOOLKIT
            var nextPageToken = m_NextPageToken;
            m_NextPageToken = null;

            if (m_Sorting != sorting || m_Complexity != complexity || m_Format != format || m_Category != category)
            {
                nextPageToken = null;
                foreach (var asset in data)
                {
                    RecycleGridItem(asset);
                }

                data.Clear();
                m_LastHiddenItemOffset = Mathf.Infinity;
            }

            m_Sorting = sorting;
            m_Complexity = complexity;
            m_Format = format;
            m_Category = category;
            this.GetAssetList(sorting, complexity, format, category, k_RequestSize, data, SetNextPageToken, nextPageToken);
#endif
        }

        void SetNextPageToken(string nextPageToken)
        {
            m_NextPageToken = nextPageToken;
        }

        protected override void ComputeConditions()
        {
            base.ComputeConditions();

            var itemSize = m_ItemSize.Value;
            m_NumPerRow = (int)(m_Size.x / itemSize.x);
            if (m_NumPerRow < 1) // Early out if item size exceeds bounds size
                return;

            if (m_LastDataCount != m_Data.Count)
            {
                m_LastDataCount = m_Data.Count;
                m_LastHiddenItemOffset = Mathf.Infinity;
            }

            m_StartPosition = m_Extents.z * Vector3.forward + (m_Extents.x - itemSize.x * 0.5f) * Vector3.left;

            var spinnerGameObject = m_Spinner.gameObject;
            if (m_NextPageToken == null) // If no NextPageToken we are waiting on a list request
            {
                if (!spinnerGameObject.activeSelf)
                    spinnerGameObject.SetActive(true);

                m_Spinner.localPosition = -m_StartPosition + m_SpinnerOffset;
                m_Spinner.Rotate(Vector3.up, m_SpinnerSpeed * Time.deltaTime, Space.Self);
            }
            else if (spinnerGameObject.activeSelf)
            {
                spinnerGameObject.SetActive(false);
            }

            // Snap back if list scrolled too far
            m_ScrollReturn = float.MaxValue;
            if (listHeight > 0 && -m_ScrollOffset >= listHeight)
            {
                m_ScrollReturn = -listHeight + m_ScaleFactor;

                if (m_Data.Count % m_NumPerRow == 0)
                    m_ScrollReturn += itemSize.z;
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

                var offset = count / m_NumPerRow * itemSize.z;
                if (offset + scrollOffset < 0 || offset + scrollOffset > m_Size.z)
                {
                    RecycleGridItem(data);
                }
                else
                {
                    var ignored = true;
                    UpdateVisibleItem(data, order++, count, ref ignored);

#if INCLUDE_POLY_TOOLKIT
                    if (m_NextPageToken != null && count == m_Data.Count - k_RequestSize / 2)
                        RequestAssetList();
#endif
                }

                count++;
            }
        }

        void RecycleGridItem(PolyGridAsset data)
        {
            var index = data.index;
            PolyGridItem item;
            if (!m_ListItems.TryGetValue(index, out item))
                return;

            m_LastHiddenItemOffset = scrollOffset;

            m_ListItems.Remove(index);

            item.SetVisibility(false, OnRecycleCompleted);
        }

        void OnRecycleCompleted(PolyGridItem gridItem)
        {
            gridItem.gameObject.SetActive(false);
            m_TemplateDictionary[gridItem.data.template].pool.Add(gridItem);
        }

        protected override void UpdateVisibleItem(PolyGridAsset data, int order, float offset, ref bool doneSettling)
        {
            PolyGridItem item;
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

        void UpdateGridItem(PolyGridItem item, int order, int count)
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

        protected override PolyGridItem GetItem(PolyGridAsset data)
        {
            const float jitterMargin = 0.125f;
            if (Mathf.Abs(scrollOffset - m_LastHiddenItemOffset) < itemSize.z * jitterMargin) // Avoid jitter while scrolling rows in and out of view
                return null;

            var item = base.GetItem(data);

            item.transform.localPosition = m_StartPosition;

            item.scaleFactor = m_ScaleFactor;
            item.SetVisibility(true);

            return item;
        }
    }
}
