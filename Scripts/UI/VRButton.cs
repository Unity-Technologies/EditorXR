using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Extensions;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.UI
{
	public class VRButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
		const float kIconHighlightedLocalZOffset = -0.0015f;
		const string kMaterialAlphaProperty = "_Alpha";
		const string kMaterialColorTopProperty = "_ColorTop";
		const string kMaterialColorBottomProperty = "_ColorBottom";

		/// <summary>
		/// If true, highlight this button OnRayEnter
		/// </summary>
		public bool rayHighlight
		{
			get { return m_RayHighlight; }
			set { m_RayHighlight = value; }
		}
		[SerializeField]
		bool m_RayHighlight = true;

		public Sprite iconSprite
		{
			set
			{
				m_IconSprite = value;
				m_Icon.sprite = m_IconSprite;
			}
		}
		Sprite m_IconSprite;

		public Color customHighlightColor
		{
			get { return m_CustomHighlightColor; }
			set { m_CustomHighlightColor = value; }
		}

		public bool pressed
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

		public bool highlighted
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

					if (!gameObject.activeInHierarchy)
						return;

					m_HighlightCoroutine = m_Highlighted ? StartCoroutine(BeginHighlight()) : StartCoroutine(EndHighlight());
				}
			}
		}
		bool m_Highlighted;

		public bool alternateIconVisible
		{
			set
			{
				if (m_AlternateIconSprite) // Only allow sprite swapping if an alternate sprite exists
					m_Icon.sprite = value ? m_AlternateIconSprite : m_OriginalIconSprite; // If true, set the icon sprite back to the original sprite
			}
			get
			{
				return m_Icon.sprite == m_AlternateIconSprite;
			}
		}

		public Button button { get { return m_Button; } }
		[SerializeField]
		Button m_Button;

		/// <summary>
		/// The inner-button's background gradient MeshRenderer
		/// </summary>
		[SerializeField]
		MeshRenderer m_ButtonMeshRenderer;

		/// <summary>
		/// Transform-root of the contents in the icon container (icons, text, etc)
		/// </summary>
		[SerializeField]
		Transform m_IconContainer;

		/// <summary>
		/// The canvas group managing the drawing of elements in the icon container
		/// </summary>
		[SerializeField]
		CanvasGroup m_CanvasGroup;

		/// <summary>
		/// The button's text component
		/// </summary>
		[SerializeField]
		Text m_Text;

		/// <summary>
		/// The button's Image component that displays icon sprites
		/// </summary>
		[SerializeField]
		Image m_Icon;

		/// <summary>
		/// Alternate icon sprite, shown when the main icon sprite isn't
		/// If set, this button will swap icon sprites OnClick
		/// </summary>
		[SerializeField]
		Sprite m_AlternateIconSprite;

		/// <summary>
		/// The color that elements in the HighlighItems collection should inherit during the highlighted state
		/// </summary>
		[SerializeField]
		Color m_CustomHighlightColor = UnityBrandColorScheme.light;

		/// <summary>
		/// Collection of items that will change appearance during the highlighted state (color/position/etc)
		/// </summary>
		[SerializeField]
		Graphic[] m_HighlightItems;

		/// <summary>
		/// If true, use a contrasting grayscale gradient for this button's visual elements (rather than the session gradient)
		/// </summary>
		[SerializeField]
		bool m_GrayscaleGradient = false;

		[Header("Animated Reveal Settings")]
		/// <summary>
		/// If true, perform a visually animated reveal of the button's contents OnEnable
		/// </summary>
		[SerializeField]
		bool m_AnimatedReveal;

		[Tooltip("Default value is 0.25")]
		/// <summary>
		/// If AnimatedReveal is enabled, wait this duration before performing the reveal
		/// </summary>
		[SerializeField]
		[Range(0f, 2f)]
		float m_DelayBeforeReveal = 0.25f;

		UnityBrandColorScheme.GradientPair m_OriginalGradientPair;
		UnityBrandColorScheme.GradientPair m_HighlightGradientPair;
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
		Vector3 m_OriginalScale;

		// The initial button reveal coroutines, before highlighting occurs
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// The visibility & highlight coroutines
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;

		void Awake()
		{
			m_OriginalColor = m_Icon.color;
			m_OriginalIconSprite = m_Icon.sprite;
			m_ButtonMaterial = U.Material.GetMaterialClone(m_ButtonMeshRenderer);
			m_OriginalGradientPair = new UnityBrandColorScheme.GradientPair(m_ButtonMaterial.GetColor(kMaterialColorTopProperty), m_ButtonMaterial.GetColor(kMaterialColorBottomProperty));
			m_HiddenLocalScale = new Vector3(transform.localScale.x, transform.localScale.y, 0f);
			m_VisibleLocalZScale = transform.localScale.z;
			m_OriginalScale = transform.localScale;
			m_OriginalIconLocalPosition = m_IconContainer.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * kIconHighlightedLocalZOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.back * kIconHighlightedLocalZOffset;
			m_HighlightGradientPair = m_GrayscaleGradient ? UnityBrandColorScheme.grayscaleSessionGradient : UnityBrandColorScheme.sessionGradient;

			// Hookup button OnClick event if there is an alternate icon sprite set
			m_Button.onClick.AddListener(SwapIconSprite);

			// Clears/resets any non-sprite content(text) from being displayed if a sprite was set on this button
			if (m_OriginalIconSprite)
				SetContent(m_OriginalIconSprite, m_AlternateIconSprite);
			else if (!string.IsNullOrEmpty(m_Text.text))
				SetContent(m_Text.text);
		}

		void OnEnable()
		{
			if (m_AnimatedReveal)
			{
				this.StopCoroutine(ref m_VisibilityCoroutine);
				m_VisibilityCoroutine = StartCoroutine(AnimateShow());
			}
		}

		void OnDisable()
		{
			ResetState();
		}

		/// <summary>
		/// Animate the reveal of this button's visual elements
		/// </summary>
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

		/// <summary>
		/// Animate the canvas group's alpha to full opacity
		/// </summary>
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

		/// <summary>
		/// Performs the animated beginning of a button's highlighted state
		/// </summary>
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
			var topHighlightColor = m_HighlightGradientPair.a;
			var bottomHighlightColor = m_HighlightGradientPair.b;
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

		/// <summary>
		/// Performs the animated ending of a button's highlighted state
		/// </summary>
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

		/// <summary>
		/// Performs the animated transition of the icon container's visual elements to their highlighted state
		/// </summary>
		/// <param name="pressed">If true, perform pressed-state specific visual changes, as opposed to hover-state specific visuals</param>
		IEnumerator IconContainerContentsBeginHighlight(bool pressed = false)
		{
			var currentPosition = m_IconContainer.localPosition;
			var targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // forward for highlight, backward for press
			var transitionAmount = Time.unscaledDeltaTime;
			var transitionAddMultiplier = !pressed ? 2 : 5; // Faster transition in for highlight; slower for pressed highlight
			while (transitionAmount < 1)
			{
				transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier;

				foreach (var graphic in m_HighlightItems)
				{
					if (graphic)
						graphic.color = Color.Lerp(m_OriginalColor, customHighlightColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic)
					graphic.color = m_CustomHighlightColor;
			}

			m_IconContainer.localPosition = targetPosition;
			m_IconHighlightCoroutine = null;
		}

		/// <summary>
		/// Performs the animated transition of the icon container's visual elements to their non-highlighted state
		/// </summary>
		IEnumerator IconContainerContentsEndHighlight()
		{
			var currentPosition = m_IconContainer.localPosition;
			var transitionAmount = 1f;
			const float kTransitionSubtractMultiplier = 5f;
			while (transitionAmount > 0)
			{
				transitionAmount -= Time.unscaledDeltaTime * kTransitionSubtractMultiplier;

				foreach (var graphic in m_HighlightItems)
				{
					if (graphic != null)
						graphic.color = Color.Lerp(m_OriginalColor, customHighlightColor, transitionAmount);
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

		/// <summary>
		/// Enable button highlighting on ray enter if autoHighlight is true
		/// </summary>
		public void OnRayEnter(RayEventData eventData)
		{
			if (rayHighlight)
				highlighted = true;
		}

		/// <summary>
		/// Disable button highlighting on ray exit if autoHighlight is true
		/// </summary>
		public void OnRayExit(RayEventData eventData)
		{
			if (rayHighlight)
				highlighted = false;
		}

		/// <summary>
		/// Swap between the main and alternate icon-sprites
		/// </summary>
		void SwapIconSprite()
		{
			// Alternate between the main icon and the alternate icon when the button is clicked
			if (m_AlternateIconSprite)
				alternateIconVisible = !alternateIconVisible;
		}

		/// <summary>
		/// Set this button to only display the first character of a given string, instead of an icon-sprite
		/// </summary>
		/// <param name="displayedText">String for which the first character is to be displayed</param>
		void SetContent(string displayedText)
		{
			m_AlternateIconSprite = null;
			m_IconSprite = null;
			m_Text.text = displayedText.Substring(0, 1);
		}

		/// <summary>
		/// Set this button to display a sprite, instead of a text character.
		/// </summary>
		/// <param name="icon">The main icon-sprite to display</param>
		/// <param name="alternateIcon">If set, the alternate icon to display when this button is clicked</param>
		void SetContent(Sprite icon, Sprite alternateIcon = null)
		{
			m_IconSprite = icon;
			m_AlternateIconSprite = alternateIcon;
			m_Text = null;
		}

		/// <summary>
		/// Reset the state of this button
		/// </summary>
		public void ResetState()
		{
			this.StopCoroutine(ref m_IconHighlightCoroutine);
			this.StopCoroutine(ref m_HighlightCoroutine);

			ResetColors();
			transform.localScale = m_OriginalScale;
		}

		/// <summary>
		/// Set this button's gradient colors
		/// </summary>
		/// <param name="gradientPair">The gradient pair to set on this button's material</param>
		public void SetMaterialColors(UnityBrandColorScheme.GradientPair gradientPair)
		{
			m_ButtonMaterial.SetColor(kMaterialColorTopProperty, gradientPair.a);
			m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, gradientPair.b);
		}

		/// <summary>
		/// Reset the colors on this button back to their original value
		/// </summary>
		public void ResetColors()
		{
			m_ButtonMaterial.SetColor(kMaterialColorTopProperty, m_OriginalGradientPair.a);
			m_ButtonMaterial.SetColor(kMaterialColorBottomProperty, m_OriginalGradientPair.b);
		}
	}
}