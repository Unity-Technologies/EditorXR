using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Extensions;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
		const float kIconHighlightedLocalZOffset = -0.0015f;
		const string kMaterialAlphaProperty = "_Alpha";
		const string kMaterialColorTopProperty = "_ColorTop";
		const string kMaterialColorBottomProperty = "_ColorBottom";

		static Material sSharedMaterialInstance;
		static UnityBrandColorScheme.GradientPair sOriginalGradientPair;

		[SerializeField]
		MeshRenderer m_ButtonMeshRenderer;

		[SerializeField]
		CanvasGroup m_CanvasGroup;

		[SerializeField]
		Image m_Icon;

		[SerializeField]
		Transform m_IconContainer;

		[SerializeField]
		Button m_Button;

		[Header("Extras")]
		[SerializeField]
		Color m_CustomHighlightColor = UnityBrandColorScheme.light;

		[SerializeField]
		Sprite m_ClickedAlternateIconSprite;

		[SerializeField]
		Graphic[] m_HighlightItems;
		UnityBrandColorScheme.GradientPair? sHighlightGradientPair;

		Transform m_parentTransform;
		Vector3 m_IconDirection;
		Material m_ButtonMaterial;
		Vector3 m_OriginalIconLocalPosition;
		Vector3 m_HiddenLocalScale;
		Vector3 m_VisibleLocalScale;
		Vector3 m_IconHighlightedLocalPosition;
		Vector3 m_IconPressedLocalPosition;
		Vector3 m_IconLookDirection;
		Color m_OriginalColor;
		Sprite m_OriginalIconSprite;

		// The initial button reveal coroutines, before highlighting
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// The already visible, highlight coroutines
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;

		public Button button { get { return m_Button; } }

		public Quaternion visibleLocalRotation
		{
			get { return m_VisibleLocalRotation; }
			set { m_VisibleLocalRotation = value; }
		}
		Quaternion m_VisibleLocalRotation;

		public Sprite iconSprite
		{
			set
			{
				m_IconSprite = value;
				m_Icon.sprite = m_IconSprite;
			}
		}
		Sprite m_IconSprite;

		bool pressed
		{
			get { return m_Pressed; }
			set
			{
				if (m_Highlighted == false)
					value = false;
				else if (value != m_Pressed && value == true) // proceed only if value is true after previously being false
				{
					m_Pressed = value;

					StopCoroutine(ref m_IconHighlightCoroutine);

					m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight(true));
				}
			}
		}
		bool m_Pressed;

		bool highlight
		{
			set
			{
				if (m_Highlighted == value)
					return;
				else
				{
					// Stop any existing icon highlight coroutines
					StopCoroutine(ref m_IconHighlightCoroutine);

					m_Highlighted = value;

					// Stop any existing begin/end highlight coroutine
					StopCoroutine(ref m_HighlightCoroutine);

					m_HighlightCoroutine = m_Highlighted == true ? StartCoroutine(BeginHighlight()) : StartCoroutine(EndHighlight());
				}
			}
		}
		bool m_Highlighted;

		void Awake()
		{
			m_OriginalColor = m_Icon.color;
			m_ButtonMaterial = U.Material.GetMaterialClone(m_ButtonMeshRenderer);
			sOriginalGradientPair = new UnityBrandColorScheme.GradientPair (m_ButtonMaterial.GetColor(kMaterialColorTopProperty), m_ButtonMaterial.GetColor(kMaterialColorBottomProperty));
			m_VisibleLocalScale = transform.localScale;
			m_HiddenLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, 0f);

			m_OriginalIconLocalPosition = m_IconContainer.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * kIconHighlightedLocalZOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.back * kIconHighlightedLocalZOffset;

			if (sHighlightGradientPair == null)
				sHighlightGradientPair = UnityBrandColorScheme.sessionGradient;

			StopCoroutine(ref m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());

			if (m_ClickedAlternateIconSprite)
			{
				m_OriginalIconSprite = m_Icon.sprite;
				// Hookup button OnClick event if there is an alternate icon sprite set
				m_Button.onClick.AddListener(SwapIconSprite);
			}
		}

		IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, 0f);

			StopCoroutine(ref m_ContentVisibilityCoroutine);

			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			var delay = 0f;
			const float kTargetDelay = 0.5f;
			var scale = m_HiddenLocalScale;
			var smoothVelocity = Vector3.zero;
			var hiddenLocalYScale = new Vector3(m_HiddenLocalScale.x, 0f, 0f);
			while (!Mathf.Approximately(scale.z, m_VisibleLocalScale.z)) // Z axis scales during the reveal
			{
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, scale.z);

				// Perform nested delay the first time stepping through the while loop
				while (delay < kTargetDelay)
				{
					delay += Time.unscaledDeltaTime;
					yield return null;

					// Perform the button vertical button reveal, after the initial wait
					if (delay >= kTargetDelay)
					{
						delay = 0f;
						float shapedDelayLerp = 0f;
						while (delay < kTargetDelay)
						{
							delay += Time.unscaledDeltaTime;
							shapedDelayLerp = delay / kTargetDelay;
							transform.localScale = Vector3.Lerp(hiddenLocalYScale, m_HiddenLocalScale, shapedDelayLerp * shapedDelayLerp);
							yield return null;
						}
					}
				}

				// Perform the button depth reveal
				scale = Vector3.SmoothDamp(scale, m_VisibleLocalScale, ref smoothVelocity, 0.25f, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, 1);
			m_VisibilityCoroutine = null;
		}

		IEnumerator ShowContent()
		{
			m_CanvasGroup.interactable = true;

			var delay = 0f;
			const float kTargetDelay = 2.5f;
			var alpha = 0f;
			const float kTargetAlpha = 1f;
			var opacitySmoothVelocity = 1f;
			while (!Mathf.Approximately(alpha, kTargetAlpha))
			{
				m_CanvasGroup.alpha = alpha;

				while (delay < kTargetDelay)
				{
					delay += Time.unscaledDeltaTime;
					yield return null;
				}

				alpha = Mathf.SmoothDamp(alpha, kTargetAlpha, ref opacitySmoothVelocity, 0.4f, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_CanvasGroup.alpha = 1;
			m_ContentVisibilityCoroutine = null;
		}

		IEnumerator BeginHighlight()
		{
			m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight());

			var transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			float shapedTransitionAmount = 0f;
			var topColor = Color.clear;
			var bottomColor = Color.clear;
			var currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			var currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			var topHighlightColor = sHighlightGradientPair.Value.a;
			var bottomHighlightColor = sHighlightGradientPair.Value.b;
			var currentLocalScale = transform.localScale;
			var highlightedLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, m_VisibleLocalScale.z * 2);
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 3;
				shapedTransitionAmount = Mathf.Pow(transitionAmount, 2);
				transform.localScale = Vector3.Lerp(currentLocalScale, highlightedLocalScale, shapedTransitionAmount);

				topColor = Color.Lerp(currentTopColor, topHighlightColor, shapedTransitionAmount);
				bottomColor = Color.Lerp(currentBottomColor, bottomHighlightColor, shapedTransitionAmount);
				m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topColor);
				m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomColor);
				yield return null;
			}

			m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topHighlightColor);
			m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomHighlightColor);
			transform.localScale = highlightedLocalScale;
			m_HighlightCoroutine = null;
		}

		IEnumerator EndHighlight()
		{
			m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsEndHighlight());

			var transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			var shapedTransitionAmount = 0f;
			var topColor = Color.clear;
			var bottomColor = Color.clear;
			var currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			var currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			var topOriginalColor = sOriginalGradientPair.a;
			var bottomOriginalColor = sOriginalGradientPair.b;
			var currentLocalScale = transform.localScale;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 3;
				shapedTransitionAmount = Mathf.Pow(transitionAmount, 2);
				topColor = Color.Lerp(currentTopColor, topOriginalColor, shapedTransitionAmount);
				bottomColor = Color.Lerp(currentBottomColor, bottomOriginalColor, shapedTransitionAmount);

				m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topColor);
				m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomColor);

				transform.localScale = Vector3.Lerp(currentLocalScale, m_VisibleLocalScale, shapedTransitionAmount);
				yield return null;
			}

			m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topOriginalColor);
			m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomOriginalColor);
			transform.localScale = m_VisibleLocalScale;
			m_HighlightCoroutine = null;
		}

		IEnumerator IconContainerContentsBeginHighlight(bool pressed = false)
		{
			var currentPosition = m_IconContainer.localPosition;
			var targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // forward for highlight, backward for press
			var transitionAmount = Time.unscaledDeltaTime;
			var transitionAddMultiplier = pressed == false ? 2 : 5; // Faster transition in for highlight; slower for pressed highlight
			while (transitionAmount < 1)
			{
				foreach (var graphic in m_HighlightItems)
				{
					if (graphic != null)
						graphic.color = Color.Lerp(m_OriginalColor, m_CustomHighlightColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier;
				yield return null;
			}

			m_IconContainer.localPosition = targetPosition;
			m_IconHighlightCoroutine = null;
		}

		IEnumerator IconContainerContentsEndHighlight()
		{
			var currentPosition = m_IconContainer.localPosition;
			var transitionAmount = 1f;
			const float kTransitionSubtractMultiplier = 5f;//18;
			while (transitionAmount > 0)
			{
				foreach (var graphic in m_HighlightItems)
				{
					if (graphic != null)
						graphic.color = Color.Lerp(m_OriginalColor, m_CustomHighlightColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				transitionAmount -= Time.unscaledDeltaTime * kTransitionSubtractMultiplier;
				yield return null;
			}

			m_IconContainer.localPosition = m_OriginalIconLocalPosition;
			m_IconHighlightCoroutine = null;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			highlight = true;
		}

		public void OnRayExit(RayEventData eventData)
		{
			highlight = false;
		}

		void SwapIconSprite()
		{
			// Alternate between the main icon and the alternate icon when the button is clicked
			m_Icon.sprite = m_Icon.sprite == m_OriginalIconSprite ? m_ClickedAlternateIconSprite : m_OriginalIconSprite;
		}
	}
}