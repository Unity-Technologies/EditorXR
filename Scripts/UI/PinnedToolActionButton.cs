#if UNITY_EDITOR
using System;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.UI
{
	sealed class PinnedToolActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		public enum ButtonType
		{
			SelectTool,
			Close
		}

		static Color s_CloseColor = UnityBrandColorScheme.red;
		static Color s_SelectColor = UnityBrandColorScheme.green;

		const string k_MaterialAlphaProperty = "_Alpha";

		//public event Action<PinnedToolActionButton> onClick;
		//public event Action onHoverEnter;
		//public event Action onHoverExit;

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

					//m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight(true));
				}
			}
		}
		bool m_Pressed;

		public bool highlighted
		{
			get { return m_Highlighted; }
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

					//m_HighlightCoroutine = m_Highlighted ? StartCoroutine(BeginHighlight()) : StartCoroutine(EndHighlight());
				}
			}
		}
		bool m_Highlighted;

		public bool visible
		{
			set
			{
				Debug.LogWarning("Perform graceful showing/hiding of pinned tool action button visual here : start value : " + m_Visible + " - Setting to : " + value);

				if (m_Visible == value)
					return;

				m_Visible = value;

				if (value && !gameObject.activeSelf)
					gameObject.SetActive(value);

				this.RestartCoroutine(ref m_VisibilityCoroutine, value ? AnimateShow() : AnimateHide());
			}
		}
		bool m_Visible = true;  // Initialize as true in order to allow for the first set to hide the visuals.

		public ButtonType buttonType
		{
			get
			{
				return m_ButtonType;
			}

			set
			{
				m_ButtonType = value;
				m_ButtonColor = buttonType == ButtonType.SelectTool ? s_SelectColor : s_CloseColor;
				m_Background.color = m_ButtonColor;
				m_Icon.sprite = buttonType == ButtonType.Close ? m_CloseIconSprite : m_Icon.sprite;
			}
		}
		ButtonType m_ButtonType;

		[SerializeField]
		Sprite m_IconSprite;

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

		[SerializeField]
		Image m_Background;

		// Alternate icon sprite, shown when the main icon sprite isn't; If set, this button will swap icon sprites OnClick
		[SerializeField]
		Sprite m_CloseIconSprite;

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
		Color m_ButtonColor;

		// The initial button reveal coroutines, before highlighting occurs
		Coroutine m_VisibilityCoroutine;

		// The visibility & highlight coroutines
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;

		bool alternateIconVisible
		{
			set
			{
				if (m_CloseIconSprite) // Only allow sprite swapping if an alternate sprite exists
					m_Icon.sprite = value ? m_CloseIconSprite : m_OriginalIconSprite; // If true, set the icon sprite back to the original sprite
			}
			get
			{
				return m_Icon.sprite == m_CloseIconSprite;
			}
		}

		public bool rotateIcon
		{
			set
			{
				m_Icon.transform.localRotation = value ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
			}
		}

		public Action<PinnedToolActionButton> clicked { get; set; }
		//public Action hoverEnter { get; set; }
		public Action hoverExit { get; set; }

		void Awake()
		{
			m_OriginalIconSprite = m_Icon.sprite;
			//m_ButtonMaterial = MaterialUtils.GetMaterialClone(m_ButtonMeshRenderer);
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
			//if (gameObject.activeSelf) // TODO remove object shouldnt alredy be disabled
			//{
				Debug.LogWarning("Disabling PinnedToolACtionButton");
				this.StopCoroutine(ref m_IconHighlightCoroutine);
				this.StopCoroutine(ref m_HighlightCoroutine);
				this.StopCoroutine(ref m_VisibilityCoroutine);
				m_ContentContainer.gameObject.SetActive(false);
				highlighted = false;
			//}
		}

		/// <summary>
		/// Animate the reveal of this button's visual elements
		/// </summary>
		IEnumerator AnimateShow()
		{
			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var currentCanvasAlpha = m_CanvasGroup.alpha;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 2f;
				m_CanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, 1f, MathUtilsExt.SmoothInOutLerpFloat(transitionAmount));
				yield return null;
			}

			m_CanvasGroup.alpha = 1f;
			m_VisibilityCoroutine = null;
		}

		/// <summary>
		/// Animate the hiding of this button's visual elements
		/// </summary>
		IEnumerator AnimateHide()
		{
			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var currentCanvasAlpha = m_CanvasGroup.alpha;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 1.5f;
				m_CanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, 0f, MathUtilsExt.SmoothInOutLerpFloat(transitionAmount));
				yield return null;
			}

			m_CanvasGroup.alpha = 0f;
			m_VisibilityCoroutine = null;
			gameObject.SetActive(false);
		}

		/// <summary>
		/// Performs the animated beginning of a button's highlighted state
		/// </summary>
		IEnumerator BeginHighlight()
		{
			//this.StopCoroutine(ref m_IconHighlightCoroutine);
			//m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsBeginHighlight());

			Debug.LogWarning("BeginHighlight : " + buttonType.ToString());
			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var shapedTransitionAmount = 0f;
			var currentCanvasAlpha = m_CanvasGroup.alpha;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 4;
				shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
				m_CanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, 1f, shapedTransitionAmount);
				yield return null;
			}

			m_HighlightCoroutine = null;
		}

		/// <summary>
		/// Performs the animated ending of a button's highlighted state
		/// </summary>
		IEnumerator EndHighlight()
		{
			//this.StopCoroutine(ref m_IconHighlightCoroutine);
			//m_IconHighlightCoroutine = StartCoroutine(IconContainerContentsEndHighlight());

			Debug.LogWarning("EndHighlight : " + buttonType.ToString());
			const float kTargetTransitionAmount = 1f;
			var transitionAmount = Time.unscaledDeltaTime;
			var shapedTransitionAmount = 0f;
			var currentCanvasAlpha = m_CanvasGroup.alpha;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 6;
				shapedTransitionAmount = MathUtilsExt.SmoothInOutLerpFloat(transitionAmount);
				m_CanvasGroup.alpha = Mathf.Lerp(currentCanvasAlpha, 0f, shapedTransitionAmount);
				yield return null;
			}
		}
/*
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
			m_VisibilityCoroutine = null;
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

				//m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic)
					graphic.color = m_HighlightItemColor;
			}

			//m_IconContainer.localPosition = targetPosition;
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

				//m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				yield return null;
			}

			foreach (var graphic in m_HighlightItems)
			{
				if (graphic != null)
					graphic.color = m_NormalContentColor;
			}

			//m_IconContainer.localPosition = m_OriginalIconLocalPosition;
			m_IconHighlightCoroutine = null;
		}
*/
		/// <summary>
		/// Enable button highlighting on ray enter if autoHighlight is true
		/// </summary>
		public void OnPointerEnter(PointerEventData eventData)
		{
			highlighted = true;
			eventData.Use();
			//hoverEnter();
		}

		/// <summary>
		/// Disable button highlighting on ray exit if autoHighlight is true
		/// </summary>
		public void OnPointerExit(PointerEventData eventData)
		{
			highlighted = false;
			eventData.Use();
			hoverExit();
		}

		/// <summary>
		/// Raise the OnClick event when this button is clicked
		/// </summary>
		public void OnPointerClick(PointerEventData eventData)
		{
			if (m_VisibilityCoroutine != null)
				return;

			SwapIconSprite();
			clicked(this);
		}

		/// <summary>
		/// Swap between the main and alternate icon-sprites
		/// </summary>
		void SwapIconSprite()
		{
			// Alternate between the main icon and the alternate icon when the button is clicked
			if (m_CloseIconSprite)
				alternateIconVisible = !alternateIconVisible;
		}
	}
}
#endif
