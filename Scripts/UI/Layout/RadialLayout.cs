/// Credit Danny Goodayle 
/// Sourced from - http://www.justapixel.co.uk/radial-layouts-nice-and-simple-in-unity3ds-ui-system/
/// Updated by ddreaper - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.

namespace UnityEngine.UI.Extensions
{
	[AddComponentMenu("Layout/Extensions/Radial Layout")]
	public class RadialLayout : LayoutGroup
	{
		[SerializeField]
		private float m_Radius = 0.01f;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_MinAngle;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_MaxAngle = 180f;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_StartAngle;

		protected override void OnEnable()
		{
			base.OnEnable();
			CalculateRadial();
		}

		public override void SetLayoutHorizontal()
		{
		}

		public override void SetLayoutVertical()
		{
		}

		public override void CalculateLayoutInputVertical()
		{
			CalculateRadial();
		}

		public override void CalculateLayoutInputHorizontal()
		{
			CalculateRadial();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			CalculateRadial();
		}
#endif

		private void CalculateRadial()
		{
			m_Tracker.Clear();
			if (transform.childCount == 0)
				return;

			var fOffsetAngle = (m_MaxAngle - m_MinAngle) / transform.childCount;

			var fAngle = m_StartAngle;
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = (RectTransform)transform.GetChild(i);
				if (child != null)
				{
					//Adding the elements to the tracker stops the user from modifiying their positions via the editor.
					m_Tracker.Add(this, child, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot);

					var pos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
					child.localPosition = pos * m_Radius;
					//Force objects to be center aligned, this can be changed however I'd suggest you keep all of the objects with the same anchor points.
					child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
					fAngle += fOffsetAngle;
				}
			}
		}
	}
}
