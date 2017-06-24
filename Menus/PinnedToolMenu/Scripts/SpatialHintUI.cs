using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public class SpatialHintUI : MonoBehaviour
	{
		[SerializeField]
		HintIcon[] m_PrimaryHintArrows;

		[SerializeField]
		HintIcon[] m_SecondaryHintArrows;

		public bool enablePreSelectVisuals
		{
			set
			{
				var semiTransparentWhite = new Color(1f, 1f, 1f, 0.5f);
				foreach (var arrow in m_PrimaryHintArrows)
				{
					arrow.visibleColor = semiTransparentWhite;
				}

				foreach (var arrow in m_SecondaryHintArrows)
				{
					arrow.visible = true;
				}
			}
		}

		public bool enableSelectVisuals
		{
			set
			{
				if (value)
				{
					foreach (var arrow in m_PrimaryHintArrows)
					{
						arrow.visibleColor = Color.white;
					}

					foreach (var arrow in m_SecondaryHintArrows)
					{
						arrow.visible = false;
					}
				}
				else
				{
					foreach (var arrow in m_SecondaryHintArrows)
					{
						arrow.visible = false;
					}

					foreach (var arrow in m_PrimaryHintArrows)
					{
						arrow.visible = false;
					}
				}
			}
		}
	}
}