using UnityEngine.UI;

namespace UnityEngine.VR.Helpers
{
	public class GridCellSizeAdjuster : MonoBehaviour
	{
		static readonly Vector2 kTransformPivot = new Vector2(0.5f, 0.5f);

		RectTransform m_LayoutGroupTransform;

		[SerializeField]
		GridLayoutGroup m_LayoutGroup;

		[SerializeField]
		float m_XScalePadding = 0.01f;

		void Awake()
		{
			m_LayoutGroupTransform = m_LayoutGroup.transform as RectTransform;
			DriveCellSizeWithLayoutGroupTransform();
		}

		void Update()
		{
			if (m_LayoutGroupTransform.hasChanged)
				DriveCellSizeWithLayoutGroupTransform();
		}

		void DriveCellSizeWithLayoutGroupTransform()
		{
			if (!m_LayoutGroupTransform)
				return;

			m_LayoutGroup.cellSize = new Vector2(m_LayoutGroupTransform.rect.left - m_LayoutGroupTransform.rect.right + m_XScalePadding, m_LayoutGroup.cellSize.y);
		}
	}
}