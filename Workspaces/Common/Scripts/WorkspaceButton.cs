using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Modules;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
		[SerializeField]
		private MeshRenderer m_ButtonMeshRenderer;

		[SerializeField]
		private CanvasGroup m_CanvasGroup;

		[SerializeField]
		private Image m_Icon;

		[SerializeField]
		private Transform m_IconContainer;

		[SerializeField]
		private Button m_Button;

		//[SerializeField]
		//private MeshRenderer m_BorderRenderer;

		private const float m_IconHighlightedLocalZOffset = -0.0015f;

		private static Material sSharedMaterialInstance;
		private static UnityBrandColorScheme.GradientPair sOriginalGradientPair;
		
		private Transform m_ButtonMeshTransform;
		private Transform m_parentTransform;
		private Vector3 m_IconDirection;
		private Material m_BorderRendererMaterial;
		private Material m_ButtonMaterial;
		private Vector3 m_VisibleInsetLocalScale;
		private Vector3 m_OriginalIconLocalPosition;
		private Vector3 m_HiddenLocalScale;
		private Vector3 m_VisibleLocalScale;
		private Vector3 m_IconHighlightedLocalPosition;
		private Vector3 m_IconPressedLocalPosition;
		private Vector3 m_IconLookDirection;

		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// TODO DELETE THESE
		private Coroutine m_HighlightCoroutine;
		private Coroutine m_IconHighlightCoroutine;

		/*
		public Material borderRendererMaterial
		{
			get { return U.Material.GetMaterialClone(m_BorderRenderer); } // return new unique color to the RadialMenuUI for settings in each RadialMenuSlot contained in a given RadialMenu
			set
			{
				m_BorderRendererMaterial = value; // TODO delete this reference if no longer needed
				m_BorderRenderer.sharedMaterial = value;
			}
		}
		*/

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

		private Sprite m_IconSprite;
		public Sprite iconSprite
		{
			set
			{
				m_IconSprite = value;
				m_Icon.sprite = m_IconSprite;
			}
		}

		bool pressed
		{
			get { return m_Pressed; }
			set
			{
				if (m_Highlighted == false)
					value = false; // prevent pressed display if slot is not currently highlighted
				else if (value != m_Pressed && value == true) // proceed only if value is true after previously being false
				{
					Debug.LogError("<color=green>Radial Menu Slot was just pressed</color>");
					m_Pressed = value;

					if (m_IconHighlightCoroutine != null)
						StopCoroutine(m_IconHighlightCoroutine);

					m_IconHighlightCoroutine = StartCoroutine(IconBeginHighlight(true));
				}
			}
		}
		bool m_Pressed;

		bool highlight
		{
			//get { return m_Highlighted; }
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
					Debug.LogError("<color=black>m_Highlighted set to : </color>" + m_Highlighted);

					// Stop any existing begin/end highlight coroutine
					if (m_HighlightCoroutine != null)
						StopCoroutine(m_HighlightCoroutine);

					m_HighlightCoroutine = m_Highlighted == true ? StartCoroutine(BeginHighlight()) : StartCoroutine(EndHighlight());
				}
			}
		}
		bool m_Highlighted;

		private static UnityBrandColorScheme.GradientPair? sHighlightGradientPair;
		/*
		public UnityBrandColorScheme.GradientPair highlightedGradientPair
		{
			set
			{
				sHighlightGradientPair = value;
				m_BorderRendererMaterial.SetColor("_ColorTop", value.a);
				m_BorderRendererMaterial.SetColor("_ColorBottom", value.b);
			}
		}
		*/

		private void Awake()
		{
			m_ButtonMeshTransform = m_ButtonMeshRenderer.transform;
			m_ButtonMaterial = U.Material.GetMaterialClone(m_ButtonMeshRenderer);
			sOriginalGradientPair = new Utilities.UnityBrandColorScheme.GradientPair (m_ButtonMaterial.GetColor("_ColorTop"), m_ButtonMaterial.GetColor("_ColorBottom"));
			m_HiddenLocalRotation = transform.localRotation;
			m_VisibleInsetLocalScale = m_ButtonMeshTransform.localScale;
			m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_ButtonMeshTransform.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
			m_VisibleLocalScale = transform.localScale;
			m_HiddenLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, 0f);

			m_OriginalIconLocalPosition = m_IconContainer.localPosition;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.forward * m_IconHighlightedLocalZOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.back * m_IconHighlightedLocalZOffset;

			if (sHighlightGradientPair == null)
				sHighlightGradientPair = UnityBrandColorScheme.GetRandomGradient();

			if (m_VisibilityCoroutine != null)
				StopCoroutine(m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());

			//Show();
			//Debug.LogWarning("Icon original local rotation" + m_OriginalIconLocalRotation);
		}

		//public void Show()
		//{
		//	//m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale;
		//	m_Pressed = false;
		//	m_Highlighted = false;

		//}

		//public void Hide()
		//{
		//	if (gameObject.activeInHierarchy)
		//	{
		//		if (m_FadeInCoroutine != null)
		//			StopCoroutine(m_FadeInCoroutine); // stop any fade in visuals

		//		if (m_FadeOutCoroutine == null)
		//			m_FadeOutCoroutine = StartCoroutine(AnimateHide()); // perform fade if not already performing
		//	}
		//}

		private IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat("_Alpha", 0f);

			//m_ButtonMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			//m_ButtonMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);
			//m_BorderRendererMaterial.SetFloat("_Expand", 0);
			//m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale ;
			//m_IconTransform.localPosition = m_OriginalIconLocalPosition;

			const float kTargetDelay = 1f;

			if (m_ContentVisibilityCoroutine != null)
				StopCoroutine(m_ContentVisibilityCoroutine);

			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			float delay = 0f;
			Vector3 scale = m_HiddenLocalScale;
			Vector3 smoothVelocity = Vector3.zero;
			Vector3 hiddenLocalYScale = new Vector3(m_HiddenLocalScale.x, 0f, 0f);
			while (!Mathf.Approximately(scale.z, m_VisibleLocalScale.z)) // Z axis scales during the reveal
			{
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat("_Alpha", scale.z);

				// Perform nested delay the first time stepping through the while loop
				while (delay < kTargetDelay)
				{
					Debug.LogWarning(transform.localScale);
					delay += Time.unscaledDeltaTime;
					yield return null;

					// Perform the button vertical button reveal, after the initial wait
					if (delay >= kTargetDelay)
					{
						delay = 0f;
						float shapedDelayLerp = 0f;
						while (delay < kTargetDelay)
						{
							Debug.LogWarning(transform.localScale);
							delay += Time.unscaledDeltaTime;
							shapedDelayLerp = delay / kTargetDelay;
							transform.localScale = Vector3.Lerp(hiddenLocalYScale, m_HiddenLocalScale, shapedDelayLerp * shapedDelayLerp);
							yield return null;
						}
					}
				}

				// Perform the button depth reveal
				Debug.LogWarning(transform.localScale);
				scale = Vector3.SmoothDamp(scale, m_VisibleLocalScale, ref smoothVelocity, 0.25f, Mathf.Infinity, Time.unscaledDeltaTime);
				yield return null;
			}

			/*
			while (opacity < 1)
			{
				//if (orderIndex == 0)
				//transform.localScale = new Vector3(opacity, 1f, 1f);
				opacity += Time.unscaledDeltaTime;
				float opacityShaped = Mathf.Pow(opacity, opacity);

				transform.localScale = Vector3.Lerp(m_HiddenLocalScale, m_VisibleLocalScale, opacity);
				//m_BorderRendererMaterial.SetFloat("_Expand", 1 - opacityShaped);
				
				//m_CanvasGroup.alpha = opacity * 0.25f;
				//m_ButtonMaterial.SetFloat("_Alpha", opacityShaped / 4);
				//m_ButtonMeshTransform.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacity / 4);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}
			*/


			//m_BorderRendererMaterial.SetFloat("_Expand", 0);
			//m_ButtonMeshTransform.localScale = m_VisibleMenuInsetLocalScale;
			//transform.localScale = Vector3.one;

			m_ButtonMaterial.SetFloat("_Alpha", 1);
			m_VisibilityCoroutine = null;
		}

		private IEnumerator ShowContent()
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


			//float opacity = 0f;
			//float duration = 0f;
			//float positionWait = orderIndex * 0.075f;
			//while (duration < 2)
			//{
			//	duration += Time.unscaledDeltaTime / positionWait;
			//	opacity = duration / 2;
			//	opacity *= opacity;
			//	m_CanvasGroup.alpha = Mathf.Clamp01(duration - 1);
			//	m_ButtonMaterial.SetFloat("_Alpha", opacity);
			//	m_ButtonMeshTransform.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacity);
			//	yield return null;
			//}

			m_CanvasGroup.alpha = 1;
			m_ContentVisibilityCoroutine = null;
		}

		//private IEnumerator AnimateHide()
		//{
		//	m_CanvasGroup.interactable = false;
		//	m_Pressed = false;
		//	m_Highlighted = false;

		//	//if (m_HighlightCoroutine != null)
		//		//StopCoroutine(m_HighlightCoroutine);

		//	float opacity = m_ButtonMaterial.GetFloat("_Alpha");;
		//	float opacityShaped = Mathf.Pow(opacity, opacity);
		//	while (opacity > 0)
		//	{
		//		//if (orderIndex == 0)
		//		Vector3 newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
		//		transform.localScale = newScale;

		//		m_CanvasGroup.alpha = opacityShaped;
		//		m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
		//		m_ButtonMaterial.SetFloat("_Alpha", opacityShaped);
		//		m_ButtonMeshTransform.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacityShaped);
		//		opacity -= Time.unscaledDeltaTime * 1.5f;
		//		opacityShaped = Mathf.Pow(opacity, opacity);
		//		yield return null;
		//	}

		//	FadeOutCleanup();
		//	m_FadeOutCoroutine = null;
		//}

		//private void FadeOutCleanup()
		//{
		//	m_CanvasGroup.alpha = 0;
		//	m_ButtonMaterial.SetColor("_ColorTop", sOriginalGradientPair.a);
		//	m_ButtonMaterial.SetColor("_ColorBottom", sOriginalGradientPair.b);
		//	m_BorderRendererMaterial.SetFloat("_Expand", 1);
		//	m_ButtonMaterial.SetFloat("_Alpha", 0);
		//	m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale;
		//	transform.localScale = Vector3.zero;
		//}

		private IEnumerator BeginHighlight()
		{
			Debug.LogError("Starting button Highlight");

			m_IconHighlightCoroutine = StartCoroutine(IconBeginHighlight());

			float transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			float shapedTransitionAmount = 0f;
			Color topColor = Color.clear;
			Color bottomColor = Color.clear;
			Color currentTopColor = m_ButtonMaterial.GetColor("_ColorTop");
			Color currentBottomColor = m_ButtonMaterial.GetColor("_ColorBottom");
			Color topHighlightColor = sHighlightGradientPair.Value.a;
			Color bottomHighlightColor = sHighlightGradientPair.Value.b;
			Vector3 currentLocalScale = transform.localScale;
			Vector3 highlightedLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, m_VisibleLocalScale.z * 2);
			while (transitionAmount < kTargetTransitionAmount)
			{
				//Debug.Log(opacity + " - " + m_Highlighted.ToString());
				transitionAmount += Time.unscaledDeltaTime * 3;
				shapedTransitionAmount = Mathf.Pow(transitionAmount, 2);
				topColor = Color.Lerp(currentTopColor, topHighlightColor, shapedTransitionAmount);
				bottomColor = Color.Lerp(currentBottomColor, bottomHighlightColor, shapedTransitionAmount);

				//m_BorderRendererMaterial.SetFloat("_Expand", transitionAmount);
				m_ButtonMaterial.SetColor("_ColorTop", topColor);
				m_ButtonMaterial.SetColor("_ColorBottom", bottomColor);

				transform.localScale = Vector3.Lerp(currentLocalScale, highlightedLocalScale, shapedTransitionAmount);
				yield return null;
			}

			//m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_ButtonMaterial.SetColor("_ColorTop", topHighlightColor);
			m_ButtonMaterial.SetColor("_ColorBottom", bottomHighlightColor);
			transform.localScale = highlightedLocalScale;

			Debug.LogError("<color=green>Finished BEGINNING Slot Highlight</color>");

			m_HighlightCoroutine = null;
		}

		private IEnumerator EndHighlight()
		{
			Debug.LogError("Ending button Highlight");

			m_IconHighlightCoroutine = StartCoroutine(IconEndHighlight());

			float transitionAmount = Time.unscaledDeltaTime;
			const float kTargetTransitionAmount = 1f;
			float shapedTransitionAmount = 0f;
			Color topColor = Color.clear;
			Color bottomColor = Color.clear;
			Color currentTopColor = m_ButtonMaterial.GetColor("_ColorTop");
			Color currentBottomColor = m_ButtonMaterial.GetColor("_ColorBottom");
			Color topOriginalColor = sOriginalGradientPair.a;
			Color bottomOriginalColor = sOriginalGradientPair.b;
			Vector3 currentLocalScale = transform.localScale;
			while (transitionAmount < kTargetTransitionAmount)
			{
				transitionAmount += Time.unscaledDeltaTime * 3;
				shapedTransitionAmount = Mathf.Pow(transitionAmount, 2);
				topColor = Color.Lerp(currentTopColor, topOriginalColor, shapedTransitionAmount);
				bottomColor = Color.Lerp(currentBottomColor, bottomOriginalColor, shapedTransitionAmount);

				//m_BorderRendererMaterial.SetFloat("_Expand", transitionAmount);
				m_ButtonMaterial.SetColor("_ColorTop", topColor);
				m_ButtonMaterial.SetColor("_ColorBottom", bottomColor);

				transform.localScale = Vector3.Lerp(currentLocalScale, m_VisibleLocalScale, shapedTransitionAmount);
				yield return null;
			}

			//m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_ButtonMaterial.SetColor("_ColorTop", topOriginalColor);
			m_ButtonMaterial.SetColor("_ColorBottom", bottomOriginalColor);
			transform.localScale = m_VisibleLocalScale;

			Debug.LogError("<color=green>Finished ENDING Slot Highlight</color>");

			m_HighlightCoroutine = null;
		}

		//private void IconHighlight()
		//{
		//	if (m_IconHighlightCoroutine != null)
		//		StopCoroutine(m_IconHighlightCoroutine);

		//	StartCoroutine(IconHighlightAnimatedShow());
		//}

		//private void IconPressed()
		//{
		//	if (m_IconHighlightCoroutine != null)
		//		StopCoroutine(m_IconHighlightCoroutine);

		//	StartCoroutine(IconHighlightAnimatedShow(true));
		//}

		private IEnumerator IconBeginHighlight(bool pressed = false)
		{
			Debug.LogError("<color=green>Inside ICON HIGHLIGHT</color>");
			Vector3 currentPosition = m_IconContainer.localPosition;
			Vector3 targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // Raise up for highlight; lower for press
			float transitionAmount = Time.unscaledDeltaTime;
			float transitionAddMultiplier = pressed == false ? 2 : 5; // Faster transition in for standard highlight; slower for pressed highlight
			while (transitionAmount < 1)
			{
				m_IconContainer.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				transitionAmount += Time.unscaledDeltaTime * transitionAddMultiplier;
				yield return null;
			}

			m_IconContainer.localPosition = targetPosition;
			m_IconHighlightCoroutine = null;
		}

		private IEnumerator IconEndHighlight()
		{
			Debug.LogError("<color=blue>ENDING ICON HIGHLIGHT</color> : " + m_IconContainer.localPosition);

			Vector3 currentPosition = m_IconContainer.localPosition;
			float transitionAmount = 1f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
			const float kTransitionSubtractMultiplier = 5f;//18;
			while (transitionAmount > 0)
			{
				m_IconContainer.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				//Debug.LogError("transition amount : " + transitionAmount + "icon position : " + m_IconTransform.localPosition);
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