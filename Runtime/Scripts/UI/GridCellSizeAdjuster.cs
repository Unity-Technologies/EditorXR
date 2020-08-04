using UnityEngine;
using UnityEngine.UI;

namespace Unity.EditorXR.Helpers
{
    sealed class GridCellSizeAdjuster : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        GridLayoutGroup m_LayoutGroup;

        [SerializeField]
        float m_XScalePadding = 0.01f;
#pragma warning restore 649

        RectTransform m_LayoutGroupTransform;

        void Awake()
        {
            m_LayoutGroupTransform = m_LayoutGroup.transform as RectTransform;
            DriveCellSizeWithLayoutGroupTransform();
        }

        void Update()
        {
            if (m_LayoutGroupTransform.hasChanged)
            {
                DriveCellSizeWithLayoutGroupTransform();
                m_LayoutGroupTransform.hasChanged = false;
            }
        }

        void DriveCellSizeWithLayoutGroupTransform()
        {
            if (!m_LayoutGroupTransform)
                return;

            m_LayoutGroup.cellSize = new Vector2(Mathf.Abs(m_LayoutGroupTransform.rect.xMin) + Mathf.Abs(m_LayoutGroupTransform.rect.xMax) + m_XScalePadding, m_LayoutGroup.cellSize.y);
        }
    }
}
