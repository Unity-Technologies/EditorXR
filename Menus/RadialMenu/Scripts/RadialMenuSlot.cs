using System.Collections;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class RadialMenuSlot : MonoBehaviour
	{
		static readonly Vector3 kHiddenLocalScale = new Vector3(1f, 0f, 1f);
		const float m_IconHighlightedLocalYOffset = 0.006f;
		const string kMaterialAlphaProperty = "_Alpha";
		const string kMaterialExpandProperty = "_Expand";
		const string kMaterialColorTopProperty = "_ColorTop";
		const string kMaterialColorBottomProperty = "_ColorBottom";

		[SerializeField]
		MeshRenderer m_InsetMeshRenderer;

		[SerializeField]
		Transform m_MenuInset;

		[SerializeField]
		CanvasGroup m_CanvasGroup;

		[SerializeField]
		Image m_Icon;

		[SerializeField]
		Transform m_IconContainer;

		[SerializeField]
		Button m_Button;

		[SerializeField]
		MeshRenderer m_BorderRenderer;

		public bool pressed
		{
			get { return m_Pressed; }
			set
			{
				// Proceed only if value is true after previously being false
				if (m_Highlighted && value != m_Pressed && value)
				{
					m_Pressed = value;

					this.StopCoroutine(ref m_IconHighlightCoroutine);

					// Don't begin a new icon highlight coroutine; Allow the currently running coroutine to finish itself according to the m_Highlighted value
					SetIconPressed();
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

				this.StopCoroutine(ref m_IconHighlightCoroutine);

				m_Highlighted = value;
				if (m_Highlighted)
				{
					// Only start the highlight coroutine if the highlight coroutine isnt already playing. Otherwise allow it to gracefully finish.
					if (m_HighlightCoroutine == null)
						m_HighlightCoroutine = StartCoroutine(Highlight());
				}
				else
					m_IconHighlightCoroutine = StartCoroutine(IconEndHighlight());
			}
		}
		bool m_Highlighted;

		GradientPair m_OriginalInsetGradientPair;
		Material m_BorderRendererMaterial;
		Transform m_IconTransform;
		Material m_InsetMaterial;
		Vector3 m_VisibleInsetLocalScale;
		Vector3 m_HiddenInsetLocalScale;
		Vector3 m_HighlightedInsetLocalScale;
		Vector3 m_OriginalIconLocalPosition;
		Vector3 m_IconHighlightedLocalPosition;
		Vector3 m_IconPressedLocalPosition;
		float m_IconLookForwardOffset = 0.5f;
		Vector3 m_IconLookDirection;

		Coroutine m_VisibilityCoroutine;
		Coroutine m_HighlightCoroutine;
		Coroutine m_IconHighlightCoroutine;
		
		public Material borderRendererMaterial
		{
			get { return U.Material.GetMaterialClone(m_BorderRenderer); } // return new unique color to the RadialMenuUI for settings in each RadialMenuSlot contained in a given RadialMenu
			set
			{
				m_BorderRendererMaterial = value;
				m_BorderRenderer.sharedMaterial = value;
			}
		}

		public int orderIndex { get; set; }

		public Button button { get { return m_Button; } }

		public static Quaternion hiddenLocalRotation { get; set; } // All menu slots share the same hidden location

		public Quaternion visibleLocalRotation { get; set; }

		public Sprite icon { set { m_Icon.sprite = value; } get { return m_Icon.sprite; } }

		public GradientPair gradientPair
		{
			set
			{
				s_GradientPair = value;
				m_BorderRendererMaterial.SetColor(kMaterialColorTopProperty, value.a);
				m_BorderRendererMaterial.SetColor(kMaterialColorBottomProperty, value.b);
			}
		}
		static GradientPair s_GradientPair;

		void Awake()
		{
			m_InsetMaterial = U.Material.GetMaterialClone(m_InsetMeshRenderer);
			m_OriginalInsetGradientPair = new GradientPair(m_InsetMaterial.GetColor(kMaterialColorTopProperty), m_InsetMaterial.GetColor(kMaterialColorBottomProperty));
			hiddenLocalRotation = transform.localRotation;
			m_VisibleInsetLocalScale = m_MenuInset.localScale;
			m_HighlightedInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_VisibleInsetLocalScale.y * 1.1f, m_VisibleInsetLocalScale.z);
			m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_MenuInset.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
			m_HiddenInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, 0f, m_VisibleInsetLocalScale.z);

			m_IconTransform = m_IconContainer;
			m_OriginalIconLocalPosition = m_IconTransform.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * m_IconHighlightedLocalYOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * -m_IconHighlightedLocalYOffset;
		}

		void OnDisable()
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);
			this.StopCoroutine(ref m_HighlightCoroutine);
			this.StopCoroutine(ref m_IconHighlightCoroutine);
		}

		public void Show()
		{
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			m_Pressed = false;
			m_Highlighted = false;

			this.StopCoroutine(ref m_VisibilityCoroutine);
		
			m_VisibilityCoroutine = StartCoroutine(AnimateShow());
		}

		public void Hide()
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateHide());
		}

		void CorrectIconRotation()
		{
			m_IconLookDirection = m_Icon.transform.position + transform.parent.forward * m_IconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			m_IconTransform.LookAt(m_IconLookDirection);
			m_IconTransform.localEulerAngles = new Vector3(0f, m_IconTransform.localEulerAngles.y, 0f);
		}

		IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_InsetMaterial.SetFloat(kMaterialAlphaProperty, 0);
			m_InsetMaterial.SetColor(kMaterialColorTopProperty, m_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor(kMaterialColorBottomProperty, m_OriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, 0);
			m_MenuInset.localScale = m_HiddenInsetLocalScale ;
			transform.localScale = kHiddenLocalScale;
			m_IconTransform.localPosition = m_OriginalIconLocalPosition;

			StartCoroutine(ShowInset());

			var opacity = 0f;
			var positionWait = orderIndex * 0.05f;
			while (opacity < 1)
			{
				opacity += Time.unscaledDeltaTime / positionWait;
				var opacityShaped = Mathf.Pow(opacity, opacity);

				transform.localScale = Vector3.Lerp(kHiddenLocalScale, Vector3.one, opacity);
				m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, 1 - opacityShaped);
				CorrectIconRotation();
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, 0);
			m_CanvasGroup.interactable = true;
			transform.localScale = Vector3.one;

			CorrectIconRotation();

			m_VisibilityCoroutine = null;
		}

		IEnumerator ShowInset()
		{
			m_CanvasGroup.alpha = 0.0001f;

			var duration = 0f;
			var positionWait = (orderIndex + 1) * 0.075f;
			while (duration < 2)
			{
				duration += Time.unscaledDeltaTime / positionWait;
				var opacity = duration / 2;
				opacity *= opacity;
				m_CanvasGroup.alpha = Mathf.Clamp01(duration - 1);
				m_InsetMaterial.SetFloat(kMaterialAlphaProperty, opacity);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacity);
				yield return null;
			}

			m_InsetMaterial.SetFloat(kMaterialAlphaProperty, 1);
			m_MenuInset.localScale = m_VisibleInsetLocalScale;
		}

		IEnumerator AnimateHide()
		{
			m_CanvasGroup.interactable = false;
			m_Pressed = false;
			m_Highlighted = false;

			var opacity = m_InsetMaterial.GetFloat(kMaterialAlphaProperty);;
			var opacityShaped = Mathf.Pow(opacity, opacity);
			while (opacity > 0)
			{
				var newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
				transform.localScale = newScale;

				m_CanvasGroup.alpha = opacityShaped;
				m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, opacityShaped);
				m_InsetMaterial.SetFloat(kMaterialAlphaProperty, opacityShaped);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacityShaped);
				opacity -= Time.unscaledDeltaTime * 1.5f;
				opacityShaped = Mathf.Pow(opacity, opacity);
				CorrectIconRotation();
				yield return null;
			}

			FadeOutCleanup();
			m_VisibilityCoroutine = null;
		}

		void FadeOutCleanup()
		{
			m_CanvasGroup.alpha = 0;
			m_InsetMaterial.SetColor(kMaterialColorTopProperty, m_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor(kMaterialColorBottomProperty, m_OriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, 1);
			m_InsetMaterial.SetFloat(kMaterialAlphaProperty, 0);
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			CorrectIconRotation();
			transform.localScale = Vector3.zero;
		}

		IEnumerator Highlight()
		{
			HighlightIcon();

			var opacity = Time.unscaledDeltaTime;
			var topColor = m_OriginalInsetGradientPair.a;
			var bottomColor = m_OriginalInsetGradientPair.b;
			while (opacity > 0)
			{
				if (m_Highlighted)
					opacity = Mathf.Clamp01(opacity + Time.unscaledDeltaTime * 4); // stay highlighted
				else
					opacity = Mathf.Clamp01(opacity - Time.unscaledDeltaTime * 2);


				topColor = Color.Lerp(m_OriginalInsetGradientPair.a, s_GradientPair.a, opacity * 2f);
				bottomColor = Color.Lerp(m_OriginalInsetGradientPair.b, s_GradientPair.b, opacity);

				m_InsetMaterial.SetColor(kMaterialColorTopProperty, topColor);
				m_InsetMaterial.SetColor(kMaterialColorBottomProperty, bottomColor);

				m_MenuInset.localScale = Vector3.Lerp(m_VisibleInsetLocalScale, m_HighlightedInsetLocalScale, opacity * opacity);
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat(kMaterialExpandProperty, 0);
			m_InsetMaterial.SetColor(kMaterialColorTopProperty, m_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor(kMaterialColorBottomProperty, m_OriginalInsetGradientPair.b);

			m_HighlightCoroutine = null;
		}

		void HighlightIcon()
		{
			this.StopCoroutine(ref m_IconHighlightCoroutine);
			m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow());
		}

		void SetIconPressed()
		{
			this.StopCoroutine(ref m_IconHighlightCoroutine);
			m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow(true));
		}

		IEnumerator IconHighlightAnimatedShow(bool pressed = false)
		{
			var currentPosition = m_IconTransform.localPosition;
			var targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // Raise up for highlight; lower for press
			var transitionAmount = Time.unscaledDeltaTime;
			var transitionAddMultiplier = pressed == false ? 14 : 18; // Faster transition in for standard highlight; slower for pressed highlight
			while (transitionAmount < 1)
			{
				m_IconTransform.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				transitionAmount = transitionAmount + Time.unscaledDeltaTime * transitionAddMultiplier;
				yield return null;
			}

			m_IconTransform.localPosition = targetPosition;
			m_IconHighlightCoroutine = null;
		}

		IEnumerator IconEndHighlight()
		{
			var currentPosition = m_IconTransform.localPosition;
			var transitionAmount = 1f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
			var transitionSubtractMultiplier = 5f;
			while (transitionAmount > 0)
			{
				m_IconTransform.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				transitionAmount -= Time.unscaledDeltaTime * transitionSubtractMultiplier;
				yield return null;
			}

			m_IconTransform.localPosition = m_OriginalIconLocalPosition;
			m_IconHighlightCoroutine = null;
		}
	}
}
