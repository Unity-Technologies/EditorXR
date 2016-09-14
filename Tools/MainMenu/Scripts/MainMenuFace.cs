using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Extensions;

namespace UnityEngine.VR.Menus
{
	public class MainMenuFace : MonoBehaviour, IPointerEnterHandler
	{
		private enum SnapState
		{
			AtRest,
			Snapping
		}

		[SerializeField]
		private MeshRenderer m_BorderOutline;
		[SerializeField]
		private CanvasGroup m_CanvasGroup;
		[SerializeField]
		private Text m_FaceTitle;
		[SerializeField]
		private GridLayoutGroup m_GridLayoutGroup;
		[SerializeField]
		private ScrollRect m_ScrollRect;
		[SerializeField]
		private SkinnedMeshRenderer m_TitleIcon;
		[SerializeField]
		private Button m_ScrollDetectionButton;
		[SerializeField]
		private RectTransform m_MaskRectTransform;

		private Material m_BorderOutlineMaterial;
		private Vector3 m_BorderOutlineOriginalLocalScale;
		private Transform m_BorderOutlineTransform;
		private RectTransform m_GridTransform;
		private float m_GridTopPosition;
		private List<Transform> m_MenuButtons;
		private Material m_TitleIconMaterial;
		private Coroutine m_VisibilityCoroutine;
		private Coroutine m_RotationVisualsCoroutine;

		private const float kBorderScaleMultiplier = 1.0135f;
		private const string kBottomGradientProperty = "_ColorBottom";
		private const string kTopGradientProperty = "_ColorTop";
		private readonly UnityBrandColorScheme.GradientPair kEmptyGradient = new UnityBrandColorScheme.GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);

		private SnapState m_SnapState;
		private RectTransform m_ButtonScrollTarget;
		private float m_TopTargetPosition;
		private float m_GridOriginalLocalY; // TODO cleanup

		private void Awake()
		{
			m_CanvasGroup.alpha = 0f;
			m_CanvasGroup.interactable = false;
			m_BorderOutlineMaterial = U.Material.GetMaterialClone(m_BorderOutline);
			m_BorderOutlineTransform = m_BorderOutline.transform;
			m_BorderOutlineOriginalLocalScale = m_BorderOutlineTransform.localScale;
			m_FaceTitle.text = "Not Set";
			m_TitleIconMaterial = U.Material.GetMaterialClone(m_TitleIcon);
			m_GridTransform = m_GridLayoutGroup.transform as RectTransform;
			m_GridTopPosition = m_GridTransform.anchoredPosition.y;
			m_GridOriginalLocalY = m_GridTransform.localPosition.y;

			SetGradientColors(kEmptyGradient);
		}

		public void SetFaceData(string faceName, List<Transform> buttons, UnityBrandColorScheme.GradientPair gradientPair)
		{
			if (m_MenuButtons != null && m_MenuButtons.Any())
				foreach (var button in m_MenuButtons)
					GameObject.DestroyImmediate(button);

			m_FaceTitle.text = faceName;
			m_MenuButtons = buttons;
			bool firstButtonSet = false;

			foreach (var button in buttons)
			{
				if (!firstButtonSet)
					m_TopTargetPosition = m_ScrollRect.transform.InverseTransformPoint(button.position).y; // cache the top-most button position for use in auto-scrolling

				Transform buttonTransform = button.transform;
				buttonTransform.SetParent(m_GridTransform);
				buttonTransform.localRotation = Quaternion.identity;
				buttonTransform.localScale = Vector3.one;
				buttonTransform.localPosition = Vector3.zero;
				MainMenuButton buttonComponent = button.GetComponent<MainMenuButton>();
				buttonComponent.ButtonEntered += SnapToGridItem;
			}

			SetGradientColors(gradientPair);
		}

		private void SetGradientColors(UnityBrandColorScheme.GradientPair gradientPair)
		{
			m_BorderOutlineMaterial.SetColor(kTopGradientProperty, gradientPair.a);
			m_BorderOutlineMaterial.SetColor(kBottomGradientProperty, gradientPair.b);
			m_TitleIconMaterial.SetColor(kTopGradientProperty, gradientPair.a);
			m_TitleIconMaterial.SetColor(kBottomGradientProperty, gradientPair.b);
		}

		public void Show()
		{
			m_BorderOutlineTransform.localScale = m_BorderOutlineOriginalLocalScale;
			StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(true));
		}

		public void Hide()
		{
			StopCoroutine(ref m_VisibilityCoroutine);
			m_VisibilityCoroutine = StartCoroutine(AnimateVisibility(false));
		}

		private IEnumerator AnimateVisibility(bool show)
		{
			if (m_VisibilityCoroutine != null)
				yield break;

			m_CanvasGroup.interactable = false;

			float smoothTime = show ? 0.35f : 0.125f;
			float startingOpacity = m_CanvasGroup.alpha;
			float targetOpacity = show ? 1f : 0f;
			float smoothVelocity = 0f;
			while (!Mathf.Approximately(startingOpacity, targetOpacity))
			{
				startingOpacity = Mathf.SmoothDamp(startingOpacity, targetOpacity, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				m_CanvasGroup.alpha = startingOpacity * startingOpacity;
				yield return null;
			}

			if (show)
			{
				m_CanvasGroup.interactable = true;
				m_CanvasGroup.alpha = 1f;
			}
			else
				m_TitleIcon.SetBlendShapeWeight(0, 0);

			m_VisibilityCoroutine = null;
		}

		public void BeginVisuals()
		{
			StopCoroutine(ref m_RotationVisualsCoroutine);
			m_RotationVisualsCoroutine = StartCoroutine(AnimateVisuals(true));
		}

		public void EndVisuals()
		{
			StopCoroutine(ref m_RotationVisualsCoroutine);
			m_RotationVisualsCoroutine = StartCoroutine(AnimateVisuals(false));
		}

		private IEnumerator AnimateVisuals(bool focus)
		{
			if (m_RotationVisualsCoroutine != null)
				yield break;

			Vector3 targetBorderLocalScale = focus ? m_BorderOutlineOriginalLocalScale * kBorderScaleMultiplier : m_BorderOutlineOriginalLocalScale;
			Vector3 currentBorderLocalScale = m_BorderOutlineTransform.localScale;

			float currentBlendShapeWeight = m_TitleIcon.GetBlendShapeWeight(0);
			float targetWeight = focus ? 100f : 0f;
			float smoothTime = focus ? 0.25f : 0.5f;
			const float kLerpEmphasisWeight = 0.2f;
			float smoothVelocity = 0f;
			while (!Mathf.Approximately(currentBlendShapeWeight, targetWeight))
			{
				currentBlendShapeWeight = Mathf.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				currentBorderLocalScale = Vector3.Lerp(currentBorderLocalScale, targetBorderLocalScale, currentBlendShapeWeight * kLerpEmphasisWeight);
				m_BorderOutlineTransform.localScale = currentBorderLocalScale;
				m_TitleIcon.SetBlendShapeWeight(0, currentBlendShapeWeight);
				yield return null;
			}

				m_TitleIcon.SetBlendShapeWeight(0, targetWeight);
				m_BorderOutlineTransform.localScale = targetBorderLocalScale;

			m_RotationVisualsCoroutine = null;
		}

		private void OnButtonHighlighted(PointerEventData eventData)
		{
			Transform buttonTransform = eventData.pointerEnter.transform;
			int buttonPositionIndex = buttonTransform.GetSiblingIndex();
			RectTransform nextButton = transform.parent.GetChild(buttonPositionIndex + 1) as RectTransform;
			SnapToGridItem(nextButton);
		}

		private void SnapToGridItem(Transform itemTransform)
		{
			//Canvas.ForceUpdateCanvases();
			//Vector2 snapPosition = (Vector2)m_ScrollRect.transform.InverseTransformPoint(m_GridTransform.position) - (Vector2)m_ScrollRect.transform.InverseTransformPoint(rTransform.position);
			//m_GridTransform.anchoredPosition = snapPosition;
			m_ButtonScrollTarget = itemTransform as RectTransform; ;

			if (m_SnapState == SnapState.AtRest)
			{
				StartCoroutine(AnimatedItemScroll());
			}
		}

		private IEnumerator AnimatedItemScroll()
		{
			m_SnapState = SnapState.Snapping;
			Vector2 targetPosition = (Vector2)m_ScrollRect.transform.InverseTransformPoint(m_ButtonScrollTarget.position);
			int direction = (int)Mathf.Sign(-targetPosition.y);
			float scrollSpeed = direction * 200f;
			float speedRamp = 0.1f;
			Vector2 scrollVector = Vector2.up * scrollSpeed;
			bool beyondBounds = false; 

			do // && m_GridTransform.position.y > startingYPos)// && targetPosition.y < m_TopTargetPosition)
			{
				Debug.Log(m_ButtonScrollTarget.name);
				Vector2 gridPositionBeforeScroll = m_GridTransform.anchoredPosition; // cache original grid transform position for later setting if the grid will be moved beyond the bounds

				m_GridTransform.anchoredPosition += (scrollVector * Time.unscaledDeltaTime * speedRamp);
				targetPosition = (Vector2) m_ScrollRect.transform.InverseTransformPoint(m_ButtonScrollTarget.position);
				direction = (int) Mathf.Sign(targetPosition.y);
				speedRamp += Mathf.Min(Time.unscaledDeltaTime*1.5f, 3.5f);

				Debug.Log("<color=white>Direction</color> : " + direction);
				if (direction == 1) // moving upwards
					beyondBounds = m_GridTransform.anchoredPosition.y < -1;
				else if (direction == -1) // moving downwards
					beyondBounds = m_GridTransform.anchoredPosition.y > 240f;
						// m_GridTransform.rect.bottom < m_GridLayoutGroup.preferredHeight;

				Debug.Log("<color=green>Target position : " + targetPosition.y + "</color>");
				//Debug.LogWarning("<color=green>m_GridTransform.anchoredPosition3D.y</color> : " + m_GridTransform.anchoredPosition.y);
				Debug.Log("<color=green>Grid transform anchored position : " + m_GridTransform.anchoredPosition.y + "</color>  - position : " + m_GridTransform.position.y);

				if (beyondBounds)
					Debug.Log("<color=red>Beyond Bounds</color> : " + beyondBounds + " - direction : " + direction);
				else
					Debug.Log("<color=yellow>NOT Beyond Bounds</color> : " + beyondBounds + " - direction : " + direction);

				if (beyondBounds)
				{
					m_GridTransform.anchoredPosition = gridPositionBeforeScroll;
					break;
				}
				
				Debug.Log("bottomBounds = " + (m_GridTransform.anchoredPosition.y - m_ScrollRect.content.rect.y));
				yield return null;
			}
			while (Mathf.Abs(targetPosition.y) > 100f && !beyondBounds);

			m_SnapState = SnapState.AtRest;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerEnter called on MainMenuFace</color>");
		}
	}
}