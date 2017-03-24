#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.UI
{
	sealed class PinnedToolActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		const string k_MaterialAlphaProperty = "_Alpha";

		public event Action onClick;
		public event Action onHoverEnter;
		public event Action onHoverExit;

		public bool pressed
		{
			get { return m_Pressed; }
			set
			{
				if (!m_Highlighted)
				{
					value = false;
				}
				else if (value != m_Pressed && value) // proceed only if value is true after previously being false
				{
					m_Pressed = value;

					this.StopCoroutine(ref m_IconHighlightCoroutine);

					m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight(true));
				}
			}
		}
		bool m_Pressed;

		bool highlighted
		{
			set
			{
				if (m_Highlighted == value)
				{
					return;
				}
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

		public bool visible
		{
			set
			{
				if (m_Visible == value)
					return;

				m_Visible = value;

				gameObject.SetActive(value);
				//this.StopCoroutine(ref m_VisibilityCoroutine);
				//m_VisibilityCoroutine = value ? StartCoroutine(AnimateShow()) : StartCoroutine(AnimateHide());
			}
		}
		bool m_Visible;

		[SerializeField]
		Sprite m_IconSprite;

		// The inner-button's background gradient MeshRenderer
		[SerializeField]
		MeshRenderer m_ButtonMeshRenderer;

		// Transform-root of the contents in the icon container (icons, text, etc)
		[SerializeField]
		Transform m_IconContainer;

		// Transform-root of the contents that will be scaled when button is highlighted
		[SerializeField]
		Transform m_ContentContainer;

		// The canvas group managing the drawing of elements in the icon container
		[SerializeField]
		CanvasGroup m_CanvasGroup;

		[SerializeField]
		Image m_Icon;

		// Alternate icon sprite, shown when the main icon sprite isn't; If set, this button will swap icon sprites OnClick
		[SerializeField]
		Sprite m_AlternateIconSprite;

		[SerializeField]
		Color m_NormalContentColor;

		// The color that elements in the HighlighItems collection should inherit during the highlighted state
		[SerializeField]
		Color m_HighlightItemColor = UnityBrandColorScheme.light;

		// Collection of items that will change appearance during the highlighted state (color/position/etc)
		[SerializeField]
		Graphic[] m_HighlightItems;

		[Header("Animated Reveal Settings")]
		[Tooltip("Default value is 0.25")]
		// If AnimatedReveal is enabled, wait this duration before performing the reveal
		[SerializeField]
		[Range(0f, 2f)]
		float m_DelayBeforeReveal = 0.25f;

		Material m_ButtonMaterial;
		Vector3 m_OriginalIconLocalPosition;
		Vector3 m_IconHighlightedLocalPosition;
		Vector3 m_IconPressedLocalPosition;
		Sprite m_OriginalIconSprite;
		Vector3 m_OriginalLocalScale;

		// The initial button reveal coroutines, before highlighting occurs
		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// The visibility & highlight coroutines
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;

		bool alternateIconVisible
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

		void Awake()
		{
			m_OriginalIconSprite = m_Icon.sprite;
			m_ButtonMaterial = MaterialUtils.GetMaterialClone(m_ButtonMeshRenderer);
			m_OriginalLocalScale = transform.localScale;
			m_OriginalIconLocalPosition = m_IconContainer.localPosition;

			m_Icon.color = m_NormalContentColor;
		}

		void OnEnable()
		{
			m_ContentContainer.gameObject.SetActive(true);
		}

		void OnDisable()
		{
			if (!gameObject.activeInHierarchy)
			{
				this.StopCoroutine(ref m_IconHighlightCoroutine);
				this.StopCoroutine(ref m_HighlightCoroutine);
				this.StopCoroutine(ref m_ContentVisibilityCoroutine);
				m_ContentContainer.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Animate the reveal of this button's visual elements
		/// </summary>
		IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);

			this.StopCoroutine(ref m_ContentVisibilityCoroutine);
			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			const float kScaleRevealDuration = 0.25f;
			var delay = 0f;
			var scale = Vector3.zero;
			var smoothVelocity = Vector3.zero;
			var currentDuration = 0f;
			var totalDuration = m_DelayBeforeReveal + kScaleRevealDuration;
			var visibleLocalScale = m_OriginalLocalScale;
			while (currentDuration < totalDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, scale.y);

				// Perform initial delay
				while (delay < m_DelayBeforeReveal)
				{
					delay += Time.unscaledDeltaTime;
					yield return null;
				}

				// Perform the button depth reveal
				scale = MathUtilsExt.SmoothDamp(scale, visibleLocalScale, ref smoothVelocity, kScaleRevealDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 1f);
			transform.localScale = m_OriginalLocalScale;
			m_VisibilityCoroutine = null;
		}

		/// <summary>
		/// Animate the hiding of this button's visual elements
		/// </summary>
		IEnumerator AnimateHide()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);

			const float kTotalDuration = 0.25f;
			var scale = transform.localScale;
			var smoothVelocity = Vector3.zero;
			var hiddenLocalScale = Vector3.zero;
			var currentDuration = 0f;
			while (currentDuration < kTotalDuration)
			{
				currentDuration += Time.unscaledDeltaTime;
				scale = MathUtilsExt.SmoothDamp(scale, hiddenLocalScale, ref smoothVelocity, kTotalDuration, Mathf.Infinity, Time.unscaledDeltaTime);
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, scale.z);

				yield return null;
			}

			m_ButtonMaterial.SetFloat(k_MaterialAlphaProperty, 0f);
			transform.localScale = hiddenLocalScale;
			m_VisibilityCoroutine = null;
			gameObject.SetActive(false);
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

				alpha = MathUtilsExt.SmoothDamp(alpha, kTargetAlpha, ref opacitySmoothVelocity, targetDuration, Mathf.Infinity, Time.unscaledDeltaTime);
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
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 4;
				shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
				yield return null;
			}

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
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 6;
				shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);

				yield return null;
			}

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
						graphic.color = Color.Lerp(m_NormalContentColor, m_HighlightItemColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic)
					graphic.color = m_HighlightItemColor;
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
						graphic.color = Color.Lerp(m_NormalContentColor, m_HighlightItemColor, transitionAmount);
				}

				m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic != null)
					graphic.color = m_NormalContentColor;
			}

			m_IconContainer.localPosition = m_OriginalIconLocalPosition;
			m_IconHighlightCoroutine = null;
		}

		/// <summary>
		/// Enable button highlighting on ray enter if autoHighlight is true
		/// </summary>
		public void OnPointerEnter(PointerEventData eventData)
		{
			highlighted = true;
			onHoverEnter();
			eventData.Use();
		}

		/// <summary>
		/// Disable button highlighting on ray exit if autoHighlight is true
		/// </summary>
		public void OnPointerExit(PointerEventData eventData)
		{
			highlighted = false;
			onHoverExit();
			eventData.Use();
		}

		/// <summary>
		/// Raise the OnClick event when this button is clicked
		/// </summary>
		public void OnPointerClick(PointerEventData eventData)
		{
			SwapIconSprite();
			onClick();
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
	}
}
#endif
