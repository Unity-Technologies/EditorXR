#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuFace : MonoBehaviour
	{
		[SerializeField]
		MeshRenderer m_BorderOutline;

		[SerializeField]
		CanvasGroup m_CanvasGroup;

		[SerializeField]
		Text m_FaceTitle;

		[SerializeField]
		Transform m_GridTransform;

		[SerializeField]
		SkinnedMeshRenderer m_TitleIcon;

		[SerializeField]
		ScrollRect m_ScrollRect;

		Material m_BorderOutlineMaterial;
		Vector3 m_BorderOutlineOriginalLocalScale;
		Transform m_BorderOutlineTransform;
		List<Transform> m_MenuButtons;
		Material m_TitleIconMaterial;
		Coroutine m_VisibilityCoroutine;

		const string k_BottomGradientProperty = "_ColorBottom";
		const string k_TopGradientProperty = "_ColorTop";
		readonly GradientPair k_EmptyGradient = new GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);

		public GradientPair gradientPair { get; private set; }

		void Awake()
		{
			m_CanvasGroup.alpha = 0f;
			m_CanvasGroup.interactable = false;
			m_BorderOutlineMaterial = MaterialUtils.GetMaterialClone(m_BorderOutline);
			m_BorderOutlineTransform = m_BorderOutline.transform;
			m_BorderOutlineOriginalLocalScale = m_BorderOutlineTransform.localScale;
			m_FaceTitle.text = "Not Set";
			m_TitleIconMaterial = MaterialUtils.GetMaterialClone(m_TitleIcon);

			SetGradientColors(k_EmptyGradient);
		}

		public void SetFaceData(string faceName, List<Transform> buttons, GradientPair gradientPair)
		{
			if (m_MenuButtons != null && m_MenuButtons.Any())
			{
				foreach (var button in m_MenuButtons)
				{
					ObjectUtils.Destroy(button);
				}
			}

			m_FaceTitle.text = faceName;
			m_MenuButtons = buttons;

			foreach (var button in buttons)
			{
				var buttonTransform = button.transform;
				buttonTransform.SetParent(m_GridTransform);
				buttonTransform.localRotation = Quaternion.identity;
				buttonTransform.localScale = Vector3.one;
				buttonTransform.localPosition = Vector3.zero;
			}

			SetGradientColors(gradientPair);
		}

		void SetGradientColors(GradientPair gradientPair)
		{
			this.gradientPair = gradientPair;
			m_BorderOutlineMaterial.SetColor(k_TopGradientProperty, gradientPair.a);
			m_BorderOutlineMaterial.SetColor(k_BottomGradientProperty, gradientPair.b);
			m_TitleIconMaterial.SetColor(k_TopGradientProperty, gradientPair.a);
			m_TitleIconMaterial.SetColor(k_BottomGradientProperty, gradientPair.b);
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

		IEnumerator AnimateVisibility(bool show)
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
				startingOpacity = MathUtilsExt.SmoothDamp(startingOpacity, targetOpacity, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
				currentDuration += Time.deltaTime;
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
	}
}
#endif
