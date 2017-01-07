using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Experimental.EditorVR.Extensions;
using UnityEngine.Experimental.EditorVR.Helpers;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEngine.Experimental.EditorVR.Menus
{
	public class MainMenuFace : MonoBehaviour
	{
		[SerializeField]
		private MeshRenderer m_BorderOutline;
		[SerializeField]
		private CanvasGroup m_CanvasGroup;
		[SerializeField]
		private Text m_FaceTitle;
		[SerializeField]
		private Transform m_GridTransform;
		[SerializeField]
		private SkinnedMeshRenderer m_TitleIcon;
		[SerializeField]
		ScrollRect m_ScrollRect;

		private Material m_BorderOutlineMaterial;
		private Vector3 m_BorderOutlineOriginalLocalScale;
		private Transform m_BorderOutlineTransform;
		private List<Transform> m_MenuButtons;
		private Material m_TitleIconMaterial;
		private Coroutine m_VisibilityCoroutine;
		private Coroutine m_RotationVisualsCoroutine;
		
		private const float kBorderScaleMultiplier = 1.0135f;
		private const string kBottomGradientProperty = "_ColorBottom";
		private const string kTopGradientProperty = "_ColorTop";
		private readonly GradientPair kEmptyGradient = new GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);

		private void Awake()
		{
			m_CanvasGroup.alpha = 0f;
			m_CanvasGroup.interactable = false;
			m_BorderOutlineMaterial = U.Material.GetMaterialClone(m_BorderOutline);
			m_BorderOutlineTransform = m_BorderOutline.transform;
			m_BorderOutlineOriginalLocalScale = m_BorderOutlineTransform.localScale;
			m_FaceTitle.text = "Not Set";
			m_TitleIconMaterial = U.Material.GetMaterialClone(m_TitleIcon);

			SetGradientColors(kEmptyGradient);
		}

		public void SetFaceData(string faceName, List<Transform> buttons, GradientPair gradientPair)
		{
			if (m_MenuButtons != null && m_MenuButtons.Any())
				foreach (var button in m_MenuButtons)
					GameObject.DestroyImmediate(button);

			m_FaceTitle.text = faceName;
			m_MenuButtons = buttons;

			foreach (var button in buttons)
			{
				Transform buttonTransform = button.transform;
				buttonTransform.SetParent(m_GridTransform);
				buttonTransform.localRotation = Quaternion.identity;
				buttonTransform.localScale = Vector3.one;
				buttonTransform.localPosition = Vector3.zero;
			}

			SetGradientColors(gradientPair);
		}

		private void SetGradientColors(GradientPair gradientPair)
		{
			m_BorderOutlineMaterial.SetColor(kTopGradientProperty, gradientPair.a);
			m_BorderOutlineMaterial.SetColor(kBottomGradientProperty, gradientPair.b);
			m_TitleIconMaterial.SetColor(kTopGradientProperty, gradientPair.a);
			m_TitleIconMaterial.SetColor(kBottomGradientProperty, gradientPair.b);
		}

		public void Show()
		{
			m_BorderOutlineTransform.localScale = m_BorderOutlineOriginalLocalScale;
			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(true));
		}

		public void Hide()
		{
			this.StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(false));
		}

		private IEnumerator AnimateVisibility(bool show)
		{
			if (m_VisibilityCoroutine != null)
				yield break;

			m_CanvasGroup.interactable = false;
			
			var smoothTime = show ? 0.35f : 0.125f;
			var startingOpacity = m_CanvasGroup.alpha;
			var targetOpacity = show ? 1f : 0f;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (currentDuration < smoothTime)
			{
				startingOpacity = U.Math.SmoothDamp(startingOpacity, targetOpacity, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				m_CanvasGroup.alpha = startingOpacity * startingOpacity;
				yield return null;
			}

			m_CanvasGroup.alpha = targetOpacity;

			if (show)
				m_CanvasGroup.interactable = true;
			else
				m_TitleIcon.SetBlendShapeWeight(0, 0);

			m_VisibilityCoroutine = null;
		}

		public void BeginVisuals()
		{
			this.StopCoroutine(ref m_RotationVisualsCoroutine);
			m_RotationVisualsCoroutine = StartCoroutine(AnimateVisuals(true));
		}

		public void EndVisuals()
		{
			this.StopCoroutine(ref m_RotationVisualsCoroutine);
			m_RotationVisualsCoroutine = StartCoroutine(AnimateVisuals(false));
		}

		private IEnumerator AnimateVisuals(bool focus)
		{
			if (m_RotationVisualsCoroutine != null)
				yield break;

			Vector3 targetBorderLocalScale = focus ? m_BorderOutlineOriginalLocalScale * kBorderScaleMultiplier : m_BorderOutlineOriginalLocalScale;
			Vector3 currentBorderLocalScale = m_BorderOutlineTransform.localScale;

			const float kLerpEmphasisWeight = 0.2f;
			var currentBlendShapeWeight = m_TitleIcon.GetBlendShapeWeight(0);
			var targetWeight = focus ? 100f : 0f;
			var smoothTime = focus ? 0.25f : 0.5f;
			var smoothVelocity = 0f;
			var currentDuration = 0f;
			while (currentDuration < smoothTime)
			{
				currentBlendShapeWeight = U.Math.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentDuration += Time.unscaledDeltaTime;
				currentBorderLocalScale = Vector3.Lerp(currentBorderLocalScale, targetBorderLocalScale, currentBlendShapeWeight * kLerpEmphasisWeight);
				m_BorderOutlineTransform.localScale = currentBorderLocalScale;
				m_TitleIcon.SetBlendShapeWeight(0, currentBlendShapeWeight);
				yield return null;
			}

			m_TitleIcon.SetBlendShapeWeight(0, targetWeight);
			m_BorderOutlineTransform.localScale = targetBorderLocalScale;

			m_RotationVisualsCoroutine = null;
		}
	}
}