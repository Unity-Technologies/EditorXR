using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
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
		Graphic[] m_HighlightItems;

		const float kIconHighlightedLocalZOffset = -0.0015f;
		const string kMaterialAlphaProperty = "_Alpha";
		const string kMaterialColorTopProperty = "_ColorTop";
		const string kMaterialColorBottomProperty = "_ColorBottom";

		static Material sSharedMaterialInstance;
		static UnityBrandColorScheme.GradientPair sOriginalGradientPair;
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

					if (m_IconHighlightCoroutine != null)
						StopCoroutine(m_IconHighlightCoroutine);

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
					if (m_IconHighlightCoroutine != null)
						StopCoroutine(m_IconHighlightCoroutine);

					m_Highlighted = value;

					// Stop any existing begin/end highlight coroutine
					if (m_HighlightCoroutine != null)
						StopCoroutine(m_HighlightCoroutine);

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

			if (m_VisibilityCoroutine != null)
				StopCoroutine(m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());
		}

		IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat(kMaterialAlphaProperty, 0f);

			if (m_ContentVisibilityCoroutine != null)
				StopCoroutine(m_ContentVisibilityCoroutine);

			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			float delay = 0f;
			const float kTargetDelay = 1f;
			Vector3 scale = m_HiddenLocalScale;
			Vector3 smoothVelocity = Vector3.zero;
			Vector3 hiddenLocalYScale = new Vector3(m_HiddenLocalScale.x, 0f, 0f);
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

			float delay = 0f;
			const float kTargetDelay = 2.5f;
			float alpha = 0f;
			const float kTargetAlpha = 1f;
			float opacitySmoothVelocity = 1f;
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

			float transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			float shapedTransitionAmount = 0f;
			Color topColor = Color.clear;
			Color bottomColor = Color.clear;
			Color currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			Color currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			Color topHighlightColor = sHighlightGradientPair.Value.a;
			Color bottomHighlightColor = sHighlightGradientPair.Value.b;
			Vector3 currentLocalScale = transform.localScale;
			Vector3 highlightedLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, m_VisibleLocalScale.z * 2);
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

			float transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			float shapedTransitionAmount = 0f;
			Color topColor = Color.clear;
			Color bottomColor = Color.clear;
			Color currentTopColor = m_ButtonMaterial.GetColor(kMaterialColorTopProperty);
			Color currentBottomColor = m_ButtonMaterial.GetColor(kMaterialColorBottomProperty);
			Color topOriginalColor = sOriginalGradientPair.a;
			Color bottomOriginalColor = sOriginalGradientPair.b;
			Vector3 currentLocalScale = transform.localScale;
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
			Vector3 currentPosition = m_IconContainer.localPosition;
			Vector3 targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // forward for highlight, backward for press
			float transitionAmount = Time.unscaledDeltaTime;
			float transitionAddMultiplier = pressed == false ? 2 : 5; // Faster transition in for highlight; slower for pressed highlight
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
			Vector3 currentPosition = m_IconContainer.localPosition;
			float transitionAmount = 1f;
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
	}
}