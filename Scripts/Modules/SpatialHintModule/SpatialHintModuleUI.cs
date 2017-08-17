#if UNITY_EDITOR
using System.Collections;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	public class SpatialHintModuleUI : MonoBehaviour, IUsesViewerScale, IControlHaptics, IRayToNode
	{
		readonly Color k_PrimaryArrowColor = Color.white;

		[Header("Scroll Visuals")]
		[SerializeField]
		CanvasGroup m_ScrollVisualsCanvasGroup;

		[SerializeField]
		HintIcon m_ScrollVisualsDragSourceArrow;

		[SerializeField]
		HintIcon m_ScrollVisualsDragTargetArrow;

		[SerializeField]
		HintLine m_ScrollHintLine;

		[SerializeField]
		HapticPulse m_ScrollBarDefineHapticPulse; // Haptic pulse performed when dragging out the spatial scroll bar

		[Header("Primary Directional Visuals")]
		[SerializeField]
		HintIcon[] m_PrimaryDirectionalHintArrows;

		[SerializeField]
		HintIcon[] m_SecondaryDirectionalHintArrows;

		bool m_Visible;
		bool m_PreScrollVisualsVisible;
		bool m_PreScrollArrowsVisible;
		bool m_SecondaryArrowsVisible;
		Vector3 m_ScrollVisualsRotation;
		Transform m_ScrollVisualsTransform;
		Coroutine m_ScrollVisualsVisibilityCoroutine;
		Transform m_ScrollVisualsDragTargetArrowTransform;

		public bool visible
		{
			get { return m_Visible; }
			set
			{
				Debug.LogError("<color=orange>Setting Hint Module visibility to : </color>" + value);
				m_Visible = value;

				if (m_Visible)
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

					scrollVisualsRotation = Vector3.zero;
				}
			}
		}

		public bool scrollVisualsVisible
		{
			set
			{
				if (value)
				{
					Debug.LogError("scrollVisualsRotation was set to a Vec3 non-zero value' beginning showing of scroll visuals : " + m_ScrollVisualsRotation);
					this.RestartCoroutine(ref m_ScrollVisualsVisibilityCoroutine, ShowScrollVisuals());
				}
			}
		}

		public bool preScrollArrowsVisible
		{
			get { return m_PreScrollArrowsVisible; }
			set
			{
				m_PreScrollArrowsVisible = value;
				if (m_PreScrollArrowsVisible)
				{
					transform.localScale = Vector3.one * this.GetViewerScale();
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

		public bool secondaryArrowsVisible
		{
			get { return m_SecondaryArrowsVisible; }
			set
			{
				m_SecondaryArrowsVisible = value;

				foreach (var arrow in m_SecondaryDirectionalHintArrows)
				{
					arrow.visible = value;
				}
			}
		}

		private bool scrollArrowsVisible
		{
			set
			{
				m_ScrollVisualsDragSourceArrow.visible = value;
				m_ScrollVisualsDragTargetArrow.visible = value;
			}
		}

		/// <summary>
		/// If non-null, enable and set the world rotation of the scroll visuals
		/// </summary>
		public Vector3 scrollVisualsRotation
		{
			get { return m_ScrollVisualsRotation ; }
			set
			{
				if (m_ScrollVisualsRotation == value)
					return;

				m_ScrollVisualsRotation = value;
			}
		}

		Node? m_ControllingNode;
		public Node? controllingNode
		{
			get
			{
				return m_ControllingNode;
			}

			set
			{
				//if (m_ControllingNode == value.Value)
					//return;

				m_ControllingNode = value;

				if (m_ControllingNode != null)
				{
					Debug.LogError("Setting Spatial Hinting Control node to : " + m_ControllingNode);
				}
				else
				{
					scrollVisualsRotation = Vector3.zero;
					this.RestartCoroutine(ref m_ScrollVisualsVisibilityCoroutine, HideScrollVisuals());
					scrollVisualsVisible = false;
				}
			}
		}

		public Vector3 scrollVisualsDragThresholdTriggerPosition { get; set; }

		public Transform contentContainer { get { return transform; } }

		void Awake()
		{
			m_ScrollVisualsTransform = m_ScrollVisualsCanvasGroup.transform;
			m_ScrollVisualsCanvasGroup.alpha = 0f;
			//m_ScrollVisualsGameObject.SetActive(false);

			m_ScrollVisualsDragTargetArrowTransform = m_ScrollVisualsDragTargetArrow.transform;
		}

		IEnumerator ShowScrollVisuals()
		{
			Debug.LogError("<color=green>SHOWING SPATIAL SCROLL VISUALS</color> : viewscale is " + this.GetViewerScale());
			// Display two arrows denoting the positive and negative directions allow for spatial scrolling, as defined by the drag vector
			//m_ScrollVisualsGameObject.SetActive(true);
			scrollArrowsVisible = true;
			preScrollArrowsVisible = false;
			secondaryArrowsVisible = false;
			transform.localScale = Vector3.one * this.GetViewerScale();
			m_ScrollVisualsTransform.LookAt(m_ScrollVisualsRotation, Vector3.up);// CameraUtils.GetMainCamera().transform.forward); // Scroll arrows should face/billboard the user.
			m_ScrollVisualsCanvasGroup.alpha = 1f; // remove
			m_ScrollVisualsDragTargetArrowTransform.localPosition = Vector3.zero;

			const float kTargetDuration = 1f;
			var currentDuration = 0f;
			var currentLocalScale = m_ScrollVisualsTransform.localScale;
			var targetLocalScale = Vector3.one;
			var currentAlpha = m_ScrollVisualsCanvasGroup.alpha;
			var secondArrowCurrentPosition = m_ScrollVisualsDragTargetArrowTransform.position;

			while (currentDuration < kTargetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
				m_ScrollVisualsCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 1f, shapedDuration);

				// Only validate movement in the initial direction with which the user began the drag
				//var movingAwayFromSource = Vector3.Dot(normalizedScrollVisualsForward, Vector3.Normalize(scrollVisualsDragThresholdTriggerPosition - secondArrowCurrentPosition)) > 0;
				//if (movingAwayFromSource && (scrollVisualsDragThresholdTriggerPosition - secondArrowCurrentPosition).magnitude >= (m_ScrollVisualsDragTargetArrow.position - secondArrowCurrentPosition).magnitude)
					m_ScrollVisualsDragTargetArrowTransform.position = Vector3.Lerp(secondArrowCurrentPosition, scrollVisualsDragThresholdTriggerPosition, shapedDuration);

				currentDuration += Time.unscaledDeltaTime * 2f;

				m_ScrollVisualsDragTargetArrowTransform.LookAt(m_ScrollVisualsDragTargetArrowTransform.position - m_ScrollVisualsTransform.position);
				m_ScrollVisualsDragTargetArrowTransform.LookAt(m_ScrollVisualsTransform.position - m_ScrollVisualsDragTargetArrowTransform.position);
				m_ScrollVisualsTransform.localScale = Vector3.Lerp(currentLocalScale, targetLocalScale, shapedDuration);
				var lineRendererPositions = new Vector3[] { m_ScrollVisualsTransform.position, m_ScrollVisualsDragTargetArrowTransform.position };
				m_ScrollHintLine.Positions = lineRendererPositions;
				m_ScrollHintLine.LineWidth = shapedDuration * this.GetViewerScale();

				this.Pulse(controllingNode, m_ScrollBarDefineHapticPulse, 1f, 1f + 8 * currentDuration);

				yield return null;
			}

			m_ScrollVisualsCanvasGroup.alpha = 1f;
		}

		IEnumerator HideScrollVisuals()
		{
			Debug.LogError("<color=red>HIDING SPATIAL SCROLL VISUALS</color>");
			// Hide the scroll visuals
			scrollArrowsVisible = false;

			const float kTargetDuration = 1f;
			var hiddenLocalScale = Vector3.zero;
			var currentDuration = 0f;
			var currentLocalScale = m_ScrollVisualsTransform.localScale;
			var currentAlpha = m_ScrollVisualsCanvasGroup.alpha;
			while (currentDuration < kTargetDuration)
			{
				var shapedDuration = MathUtilsExt.SmoothInOutLerpFloat(currentDuration / kTargetDuration);
				m_ScrollVisualsTransform.localScale = Vector3.Lerp(currentLocalScale, hiddenLocalScale, shapedDuration);
				m_ScrollVisualsCanvasGroup.alpha = Mathf.Lerp(currentAlpha, 0f, shapedDuration);
				//m_Icon.color = Color.Lerp(currentColor, m_HiddenColor, currentDuration);
				currentDuration += Time.unscaledDeltaTime * 3.5f;
				m_ScrollHintLine.LineWidth = (1 - shapedDuration) * this.GetViewerScale();
				yield return null;
			}

			m_ScrollVisualsCanvasGroup.alpha = 0;
			m_ScrollVisualsTransform.localScale = hiddenLocalScale;
			//m_ScrollVisualsTransform.localRotation = Quaternion.identity;
			//m_ScrollVisualsGameObject.SetActive(false);
		}

		public void PulseScrollArrows()
		{
			m_ScrollVisualsDragSourceArrow.PulseColor();
			m_ScrollVisualsDragTargetArrow.PulseColor();
			m_ScrollHintLine.PulseColor();
		}
	}
}
#endif
