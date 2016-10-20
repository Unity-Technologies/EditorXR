using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;

namespace UnityEngine.VR.Menus
{
	public class RadialMenuSlot : MonoBehaviour
	{
		[SerializeField]
		private MeshRenderer m_InsetMeshRenderer;

		[SerializeField]
		private Transform m_MenuInset;

		[SerializeField]
		private CanvasGroup m_CanvasGroup;

		[SerializeField]
		private Image m_Icon;

		[SerializeField]
		private Transform m_IconContainer;

		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private MeshRenderer m_BorderRenderer;

		private const float m_IconHighlightedLocalYOffset = 0.006f;

		private static Material s_SharedInsetMaterialInstance;
		private static UnityBrandColorScheme.GradientPair s_OriginalInsetGradientPair;
		private static readonly Vector3 kHiddenLocalScale = new Vector3(1f, 0f, 1f);

		private Vector3 m_IconDirection;
		private Material m_BorderRendererMaterial;
		private Transform m_IconTransform;
		private Material m_InsetMaterial;
		private Vector3 m_VisibleInsetLocalScale;
		private Vector3 m_HiddenInsetLocalScale;
		private Vector3 m_HighlightedInsetLocalScale;
		private Vector3 m_OriginalIconLocalPosition;
		private Vector3 m_IconHighlightedLocalPosition;
		private Vector3 m_IconPressedLocalPosition;
		private float m_IconLookForwardOffset = 0.5f;
		private Vector3 m_IconLookDirection;
		
		private Coroutine m_FadeInCoroutine;
		private Coroutine m_FadeOutCoroutine;
		private Coroutine m_HighlightCoroutine;
		private Coroutine m_IconHighlightCoroutine;
		private Coroutine m_IconEndHighlightCoroutine;
		
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

		private static Quaternion m_HiddenLocalRotation; // All menu slots share the same hidden location
		public static Quaternion hiddenLocalRotation { get { return m_HiddenLocalRotation; } }

		private Quaternion m_VisibleLocalRotation;
		public Quaternion visibleLocalRotation
		{
			get { return m_VisibleLocalRotation; }
			set { m_VisibleLocalRotation = value; }
		}

		public Sprite icon
		{
			set { m_Icon.sprite = value; }
		}

		private bool m_Pressed;
		public bool pressed
		{
			get { return m_Pressed; }
			set
			{
				// Proceed only if value is true after previously being false
				if (m_Highlighted && value != m_Pressed && value)
				{
					m_Pressed = value;

					StopCoroutine(ref m_IconEndHighlightCoroutine);

					// Don't begin a new icon highlight coroutine; Allow the currently running coroutine to finish itself according to the m_Highlighted value
					StopCoroutine(ref m_IconHighlightCoroutine);

					m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow(true));
				}
			}
		}

		private bool m_Highlighted;
		public bool highlighted
		{
			set
			{
				if (m_Highlighted == value)
					return;

				StopCoroutine(ref m_IconEndHighlightCoroutine);

				m_Highlighted = value;
				if (m_Highlighted)
				{
					// Only start the highlight coroutine if the highlight coroutine isnt already playing. Otherwise allow it to gracefully finish.
					if (m_HighlightCoroutine == null)
						m_HighlightCoroutine = StartCoroutine(Highlight());
				}
				else
					m_IconEndHighlightCoroutine = StartCoroutine(IconEndHighlight());
			}
		}

		private static UnityBrandColorScheme.GradientPair s_GradientPair;
		public UnityBrandColorScheme.GradientPair gradientPair
		{
			set
			{
				s_GradientPair = value;
				m_BorderRendererMaterial.SetColor("_ColorTop", value.a);
				m_BorderRendererMaterial.SetColor("_ColorBottom", value.b);
			}
		}

		private void Awake()
		{
			m_InsetMaterial = U.Material.GetMaterialClone(m_InsetMeshRenderer);
			s_OriginalInsetGradientPair = new UnityBrandColorScheme.GradientPair (m_InsetMaterial.GetColor("_ColorTop"), m_InsetMaterial.GetColor("_ColorBottom"));
			m_HiddenLocalRotation = transform.localRotation;
			m_VisibleInsetLocalScale = m_MenuInset.localScale;
			m_HighlightedInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_VisibleInsetLocalScale.y * 1.1f, m_VisibleInsetLocalScale.z);
			m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_MenuInset.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
			m_HiddenInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, 0f, m_VisibleInsetLocalScale.z);

			m_IconTransform = m_IconContainer;
			m_OriginalIconLocalPosition = m_IconTransform.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * m_IconHighlightedLocalYOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * -m_IconHighlightedLocalYOffset;
		}

		public void Show()
		{
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			m_Pressed = false;
			m_Highlighted = false;

			StopCoroutine(ref m_FadeInCoroutine);
			StopCoroutine(ref m_FadeOutCoroutine);

			m_FadeInCoroutine = StartCoroutine(AnimateShow());
		}

		public void Hide()
		{
			StopCoroutine(ref m_FadeInCoroutine); // stop any fade in visuals

			if (m_FadeOutCoroutine == null)
				m_FadeOutCoroutine = StartCoroutine(AnimateHide()); // perform fade if not already performing
		}

		private void CorrectIconRotation()
		{
			m_IconLookDirection = m_Icon.transform.position + transform.parent.forward * m_IconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			m_IconTransform.LookAt(m_IconLookDirection);
			m_IconTransform.localEulerAngles = new Vector3(0f, m_IconTransform.localEulerAngles.y, 0f);
		}

		private IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_InsetMaterial.SetColor("_ColorTop", s_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", s_OriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat("_Expand", 0);
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
				m_BorderRendererMaterial.SetFloat("_Expand", 1 - opacityShaped);
				CorrectIconRotation();
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_CanvasGroup.interactable = true;
			transform.localScale = Vector3.one;

			CorrectIconRotation();

			m_FadeInCoroutine = null;
		}

		private IEnumerator ShowInset()
		{
			m_CanvasGroup.alpha = 0.0001f;

			var duration = 0f;
			var positionWait = orderIndex * 0.075f;
			while (duration < 2)
			{
				duration += Time.unscaledDeltaTime / positionWait;
				var opacity = duration / 2;
				opacity *= opacity;
				m_CanvasGroup.alpha = Mathf.Clamp01(duration - 1);
				m_InsetMaterial.SetFloat("_Alpha", opacity);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacity);
				yield return null;
			}

			m_InsetMaterial.SetFloat("_Alpha", 1);
			m_MenuInset.localScale = m_VisibleInsetLocalScale;
		}

		private IEnumerator AnimateHide()
		{
			m_CanvasGroup.interactable = false;
			m_Pressed = false;
			m_Highlighted = false;

			var opacity = m_InsetMaterial.GetFloat("_Alpha");;
			var opacityShaped = Mathf.Pow(opacity, opacity);
			while (opacity > 0)
			{
				var newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
				transform.localScale = newScale;

				m_CanvasGroup.alpha = opacityShaped;
				m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
				m_InsetMaterial.SetFloat("_Alpha", opacityShaped);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacityShaped);
				opacity -= Time.unscaledDeltaTime * 1.5f;
				opacityShaped = Mathf.Pow(opacity, opacity);
				CorrectIconRotation();
				yield return null;
			}

			FadeOutCleanup();
			m_FadeOutCoroutine = null;
		}

		private void FadeOutCleanup()
		{
			m_CanvasGroup.alpha = 0;
			m_InsetMaterial.SetColor("_ColorTop", s_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", s_OriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat("_Expand", 1);
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			CorrectIconRotation();
			transform.localScale = Vector3.zero;
		}

		private IEnumerator Highlight()
		{
			if (m_IconHighlightCoroutine == null)
				m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow());

			var opacity = Time.unscaledDeltaTime;
			var topColor = s_OriginalInsetGradientPair.a;
			var bottomColor = s_OriginalInsetGradientPair.b;
			while (opacity > 0)
			{
				if (m_Highlighted)
				{
					if (!Mathf.Approximately(opacity, 1f))
						opacity = Mathf.Clamp01(opacity + Time.unscaledDeltaTime * 4); // stay highlighted
				}
				else
					opacity = Mathf.Clamp01(opacity - Time.unscaledDeltaTime * 2);


				topColor = Color.Lerp(s_OriginalInsetGradientPair.a, s_GradientPair.a, opacity * 2f);
				bottomColor = Color.Lerp(s_OriginalInsetGradientPair.b, s_GradientPair.b, opacity);

				m_InsetMaterial.SetColor("_ColorTop", topColor);
				m_InsetMaterial.SetColor("_ColorBottom", bottomColor);

				m_MenuInset.localScale = Vector3.Lerp(m_VisibleInsetLocalScale, m_HighlightedInsetLocalScale, opacity * opacity);
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_InsetMaterial.SetColor("_ColorTop", s_OriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", s_OriginalInsetGradientPair.b);

			m_HighlightCoroutine = null;
		}

		private void IconHighlight()
		{
			if (m_IconHighlightCoroutine != null)
				StopCoroutine(m_IconHighlightCoroutine);

			StartCoroutine(IconHighlightAnimatedShow());
		}

		private void IconPressed()
		{
			if (m_IconHighlightCoroutine != null)
				StopCoroutine(m_IconHighlightCoroutine);

			StartCoroutine(IconHighlightAnimatedShow(true));
		}

		private IEnumerator IconHighlightAnimatedShow(bool pressed = false)
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

		private IEnumerator IconEndHighlight()
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
			m_IconEndHighlightCoroutine = null;
		}
	}
}