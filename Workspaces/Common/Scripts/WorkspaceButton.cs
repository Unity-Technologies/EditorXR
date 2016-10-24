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
		Sprite m_AlternateIconSprite;

		[SerializeField]
		bool m_SwapIconsOnClick = true;

		[SerializeField]
		Graphic[] m_HighlightItems;

		[Header("Animated Reveal Settings")]
		[SerializeField]
		bool m_AnimatedReveal;

		[Tooltip("Default value is 0.5")]
		[SerializeField]
		[Range(0f, 2f)]
		float m_DelayBeforeReveal = 0.5f;

		UnityBrandColorScheme.GradientPair m_OriginalGradientPair;
		UnityBrandColorScheme.GradientPair? m_HighlightGradientPair;
		Transform m_parentTransform;
		Vector3 m_IconDirection;
		Material m_ButtonMaterial;
		Vector3 m_OriginalIconLocalPosition;
		Vector3 m_HiddenLocalScale;
		Vector3 m_IconHighlightedLocalPosition;
		Vector3 m_IconPressedLocalPosition;
		Vector3 m_IconLookDirection;
		Color m_OriginalColor;
		Sprite m_OriginalIconSprite;
		float m_VisibleLocalZScale;

		// The initial button reveal coroutines, before highlighting
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// The already visible, highlight coroutines
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;

		public Button button { get { return m_Button; } }

		public bool alternateIconVisible
		{
			set
			{
				if (m_AlternateIconSprite != null) // Only allow sprite swapping if an alternate sprite exists
					m_Icon.sprite = value ? m_AlternateIconSprite : m_OriginalIconSprite; // If true, set the icon sprite back to the original sprite
			}
		}

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
				if (!m_Highlighted)
					value = false;
				else if (value != m_Pressed && value) // proceed only if value is true after previously being false
				{
					m_Pressed = value;

					this.StopCoroutine(ref m_IconHighlightCoroutine);

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
					this.StopCoroutine(ref m_IconHighlightCoroutine);

					m_Highlighted = value;

					// Stop any existing begin/end highlight coroutine
					this.StopCoroutine(ref m_HighlightCoroutine);

					m_HighlightCoroutine = m_Highlighted ? StartCoroutine(BeginHighlight()) : StartCoroutine(EndHighlight());
				}
			}
		}
		bool m_Highlighted;

		void Awake()
		{
			m_OriginalColor = m_Icon.color;
			m_ButtonMaterial = U.Material.GetMaterialClone(m_ButtonMeshRenderer);
			m_OriginalGradientPair = new UnityBrandColorScheme.GradientPair(m_ButtonMaterial.GetColor(kMaterialColorTopProperty), m_ButtonMaterial.GetColor(kMaterialColorBottomProperty));
			m_HiddenLocalScale = new Vector3(transform.localScale.x, transform.localScale.y, 0f);
			m_VisibleLocalZScale = transform.localScale.z;

			m_OriginalIconLocalPosition = m_IconContainer.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * kIconHighlightedLocalZOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.back * kIconHighlightedLocalZOffset;

			if (m_HighlightGradientPair == null)
				m_HighlightGradientPair = UnityBrandColorScheme.sessionGradient;

			m_OriginalIconSprite = m_Icon.sprite;
			// Hookup button OnClick event if there is an alternate icon sprite set
			if (m_SwapIconsOnClick && m_AlternateIconSprite)
				m_Button.onClick.AddListener(SwapIconSprite);
		}

		void OnEnable()
		{
			if (m_AnimatedReveal)
			{
				this.StopCoroutine(ref m_VisibilityCoroutine);
				m_VisibilityCoroutine = StartCoroutine(AnimateShow());
			}
		}

		IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, 0f);

			this.StopCoroutine(ref m_ContentVisibilityCoroutine);
			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			const float kInitialRevealDuration = 0.5f;
			const float kScaleRevealDuration = 0.25f;
			var delay = 0f;
			var scale = m_HiddenLocalScale;
			var smoothVelocity = Vector3.zero;
			var hiddenLocalYScale = new Vector3(m_HiddenLocalScale.x, 0f, 0f);
			var currentDuration = 0f;
			var totalDuration = m_DelayBeforeReveal + kInitialRevealDuration + kScaleRevealDuration;
			var visibleLocalScale = new Vector3(transform.localScale.x, transform.localScale.y, m_VisibleLocalZScale);
			while (currentDuration < totalDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, scale.z);

				// Perform initial delay
				while (delay < m_DelayBeforeReveal)
				{
					delay += Time.unscaledDeltaTime;
					yield return null;	
				}

				// Perform the button vertical button reveal, after the initial wait
				while (delay < kInitialRevealDuration + m_DelayBeforeReveal)
				{
					delay += Time.unscaledDeltaTime;
					var shapedDelayLerp = delay / m_DelayBeforeReveal;
					transform.localScale = Vector3.Lerp(hiddenLocalYScale, m_HiddenLocalScale, shapedDelayLerp * shapedDelayLerp);
					yield return null;
				}

				// Perform the button depth reveal
				scale = U.Math.SmoothDamp(scale, visibleLocalScale, ref smoothVelocity, kScaleRevealDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, 1);
			m_VisibilityCoroutine = null;
		}

		IEnumerator ShowContent()
		{
			m_CanvasGroup.interactable = true;

			const float kTargetAlpha = 1f;
			const float kRevealDuration = 0.4f;
			const float kInitialDelayLengthenMultipler = 5f; // used to scale up the initial delay based on the m_InitialDelay value
			var delay = 0f;
			var targetDelay = Mathf.Clamp(m_DelayBeforeReveal * kInitialDelayLengthenMultipler, 0f, 2.5f); // scale the target delay, with a maximum clamp
			var alpha = 0f;
			var opacitySmoothVelocity = 1f;
			var currentDuration = 0f;
			var targetDuration = targetDelay + kRevealDuration;
			while (currentDuration < targetDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				m_CanvasGroup.alpha = alpha;

				while (delay < targetDelay)
				{
					delay += Time.unscaledDeltaTime;
					yield return null;
				}

				alpha = U.Math.SmoothDamp(alpha, kTargetAlpha, ref opacitySmoothVelocity, targetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_CanvasGroup.alpha = 1;
			m_ContentVisibilityCoroutine = null;
		}

		IEnumerator BeginHighlight()
		{
			this.StopCoroutine(ref m_IconHighlightCoroutine);
			m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight());

			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var shapedTransitionAmount = 0f;
			var topColor = Color.clear;
			var bottomColor = Color.clear;
			var currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			var currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			var topHighlightColor = m_HighlightGradientPair.Value.a;
			var bottomHighlightColor = m_HighlightGradientPair.Value.b;
			var currentLocalScale = transform.localScale;
			var highlightedLocalScale = new Vector3(transform.localScale.x, transform.localScale.y, m_VisibleLocalZScale * 2);
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
			this.StopCoroutine(ref m_IconHighlightCoroutine);
			m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsEndHighlight());

			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var shapedTransitionAmount = 0f;
			var topColor = Color.clear;
			var bottomColor = Color.clear;
			var currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			var currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			var topOriginalColor = m_OriginalGradientPair.a;
			var bottomOriginalColor = m_OriginalGradientPair.b;
			var currentLocalScale = transform.localScale;
			var targetScale = new Vector3(transform.localScale.x, transform.localScale.y, m_VisibleLocalZScale);
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 3;
				shapedTransitionAmount = Mathf.Pow(transitionAmount, 2);
				topColor = Color.Lerp(currentTopColor, topOriginalColor, shapedTransitionAmount);
				bottomColor = Color.Lerp(currentBottomColor, bottomOriginalColor, shapedTransitionAmount);

				m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topColor);
				m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomColor);

				transform.localScale = Vector3.Lerp(currentLocalScale, targetScale, shapedTransitionAmount);
				yield return null;
			}

			m_ButtonMaterial.SetColor(kMaterialColorTopProperty, topOriginalColor);
			m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, bottomOriginalColor);
			transform.localScale = targetScale;
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
				transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier;

				foreach (var graphic in m_HighlightItems)
				{
					if (graphic != null)
						graphic.color = Color.Lerp(m_OriginalColor, m_CustomHighlightColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic != null)
					graphic.color = m_CustomHighlightColor;
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
				transitionAmount -= Time.unscaledDeltaTime * kTransitionSubtractMultiplier;

				foreach (var graphic in m_HighlightItems)
				{
					if (graphic != null)
						graphic.color = Color.Lerp(m_OriginalColor, m_CustomHighlightColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic != null)
					graphic.color = m_OriginalColor;
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
			m_Icon.sprite = m_Icon.sprite == m_OriginalIconSprite ? m_AlternateIconSprite : m_OriginalIconSprite;
		}
	}
}