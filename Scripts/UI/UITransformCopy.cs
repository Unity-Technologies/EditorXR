using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.VR.Helpers
{
	public class UITransformCopy : UIBehaviour
	{
		static readonly Vector2 kTransformPivot = new Vector2(0.5f, 0.5f);
		const float kLayerHeight = 0.004f;
		const float kExtraSpace = 0.00055f; // To avoid Z-fighting

		Transform m_TargetTransform;

		[SerializeField]
		RectTransform m_SourceRectTransform;

		[SerializeField]
		float m_XPositionPadding = 0.005f;

		[SerializeField]
		float m_XScalePadding = 0.01f;

		protected override void Awake()
		{
			m_TargetTransform = transform;
			m_TargetTransform.SetParent(m_SourceRectTransform, false);
			DriveTransformWithRectTransform();
		}

		void Update()
		{
			if (m_SourceRectTransform.hasChanged)
				DriveTransformWithRectTransform();
		}

		void DriveTransformWithRectTransform()
		{
			if (!m_SourceRectTransform || !m_TargetTransform)
				return;

			// Drive transform with source RectTransform
			var rectSize = m_SourceRectTransform.rect.size.Abs();
			// Scale pivot by rect size to get correct xy local position
			var pivotOffset = Vector2.Scale(rectSize, kTransformPivot - m_SourceRectTransform.pivot);

			// Add space for cube
			var localPosition = m_SourceRectTransform.localPosition;
			m_SourceRectTransform.localPosition = new Vector3(localPosition.x, localPosition.y, -kLayerHeight);

			//Offset by 0.5 * height to account for pivot in center
			const float zOffset = kLayerHeight * 0.5f + kExtraSpace;
			m_TargetTransform.localPosition = new Vector3(pivotOffset.x + m_XPositionPadding, pivotOffset.y, zOffset);
			m_TargetTransform.localScale = new Vector3(rectSize.x + m_XScalePadding, rectSize.y, transform.localScale.z);
		}
	}
}