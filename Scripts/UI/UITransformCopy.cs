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

		protected void Awake()
		{
			if (m_SourceRectTransform != transform)
			{
				m_TargetTransform = transform;
				m_TargetTransform.SetParent(m_SourceRectTransform, false);
			}
			else
				Debug.LogError("A valid Transform could not be fetched ");
		}

		protected override void Start()
		{
			DriveTransformWithRectTransform();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			DriveTransformWithRectTransform();
		}

		public void DriveTransformWithRectTransform()
		{
			if (!m_SourceRectTransform || !m_TargetTransform)
				return;

			// Drive transform with source RectTransform
			const float kStandardCubeSideScalePadding = 0.01f;
			const float kStandardCubeSidePositionPadding = 0.005f;
			var rectSize = m_SourceRectTransform.rect.size.Abs();
			// Scale pivot by rect size to get correct xy local position
			var pivotOffset = Vector2.Scale(rectSize, kTransformPivot - m_SourceRectTransform.pivot);

			// Add space for cuboid
			var localPosition = m_SourceRectTransform.localPosition;
			m_SourceRectTransform.localPosition = new Vector3(localPosition.x, localPosition.y, -kLayerHeight);

			//Offset by 0.5 * height to account for pivot in center
			const float zOffset = kLayerHeight * 0.5f + kExtraSpace;
			m_TargetTransform.localPosition = new Vector3(pivotOffset.x + kStandardCubeSidePositionPadding, pivotOffset.y, zOffset);
			m_TargetTransform.localScale = new Vector3(rectSize.x + kStandardCubeSideScalePadding, rectSize.y, kLayerHeight);
		}
	}
}