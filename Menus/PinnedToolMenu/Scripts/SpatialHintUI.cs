using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public class SpatialHintUI : MonoBehaviour
	{
		readonly Color k_PrimaryArrowColor = Color.white;

		[SerializeField] [FormerlySerializedAs("m_PrimaryHintArrows")]
		HintIcon[] m_PrimaryDirectionalHintArrows;

		[SerializeField] [FormerlySerializedAs("m_SecondaryHintArrows")]
		HintIcon[] m_SecondaryDirectionalHintArrows;

		[SerializeField]
		HintIcon[] m_PrimaryRotationalHintArrows;

		[SerializeField]
		HintIcon[] m_SecondaryRotationalHintArrows;

		[SerializeField]
		CanvasGroup m_ScrollVisualsCanvasGroup;

		Vector3? m_ScrollVisualsRotation;
		Transform m_ScrollVisualsTransform;
		GameObject m_ScrollVisualsGameObject;

		/// <summary>
		/// Enables/disables the visual elements that should be shown when beginning to initiate a spatial selection action
		/// This is only enabled before the enabling of the main select visuals
		/// </summary>
		public bool enablePreviewVisuals
		{
			set
			{
				var semiTransparentWhite = new Color(1f, 1f, 1f, 0.5f);
				foreach (var arrow in m_PrimaryDirectionalHintArrows)
				{
					arrow.visibleColor = semiTransparentWhite;
				}

				foreach (var arrow in m_SecondaryDirectionalHintArrows)
				{
					arrow.visible = true;
				}
			}
		}

		public bool enablePrimaryArrowVisuals
		{
			set
			{
				if (value)
				{
					foreach (var arrow in m_PrimaryDirectionalHintArrows)
					{
						arrow.visibleColor = k_PrimaryArrowColor;
					}
				}
				else
				{
					foreach (var arrow in m_PrimaryDirectionalHintArrows)
					{
						arrow.visible = false;
					}
				}
			}
		}

		public bool enableVisuals
		{
			set
			{
				if (value)
				{
					foreach (var arrow in m_PrimaryDirectionalHintArrows)
					{
						arrow.visibleColor = k_PrimaryArrowColor;
					}

					foreach (var arrow in m_SecondaryDirectionalHintArrows)
					{
						arrow.visible = false;
					}
				}
				else
				{
					foreach (var arrow in m_PrimaryDirectionalHintArrows)
					{
						arrow.visible = false;
					}

					foreach (var arrow in m_SecondaryDirectionalHintArrows)
					{
						arrow.visible = false;
					}
				}
			}
		}

		/// <summary>
		/// If non-null, enable and set the world rotation of the scroll visuals
		/// </summary>
		public Vector3? scrollVisualsRotation
		{
			// Set null In order to hide the scroll visuals
			set
			{
				if (m_ScrollVisualsRotation == null && value == null)
					return;

				var newRotation = value;

				//if (m_ScrollVisualsRotation == newRotation)
					//return;

				m_ScrollVisualsRotation = newRotation;
				if (m_ScrollVisualsRotation != null)
				{
					Debug.LogError("<color=green>SHOWING SPATIAL SCROLL VISUALS</color>");

					// Display two arrows denoting the positive and negative directions allow for spatial scrolling, as defined by the drag vector
					m_ScrollVisualsGameObject.SetActive(true);
					m_ScrollVisualsCanvasGroup.alpha = 1f;
					//m_ScrollVisualsTransform.rotation = m_ScrollVisualsRotation.Value;
					m_ScrollVisualsTransform.LookAt(m_ScrollVisualsRotation.Value);
				}
				else
				{
					Debug.LogError("<color=red>HIDING SPATIAL SCROLL VISUALS</color>");

					// Hide the scroll visuals
					m_ScrollVisualsCanvasGroup.alpha = 1;
					m_ScrollVisualsGameObject.SetActive(false);
				}
			}
		}

		void Awake()
		{
			m_ScrollVisualsTransform = m_ScrollVisualsCanvasGroup.transform;
			m_ScrollVisualsGameObject = m_ScrollVisualsTransform.gameObject;
			m_ScrollVisualsCanvasGroup.alpha = 0f;
			m_ScrollVisualsGameObject.SetActive(false);
		}
	}
}