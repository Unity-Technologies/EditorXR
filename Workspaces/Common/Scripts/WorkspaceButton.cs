using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Workspaces
{
	public class WorkspaceButton : MonoBehaviour
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

		private const float m_IconHighlightedLocalYOffset = 0.006f;

		private static Material sSharedInsetMaterialInstance;
		private static UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair sOriginalInsetGradientPair;
		
		private Transform m_ButtonMeshTransform;
		private Transform m_parentTransform;
		private Vector3 m_IconDirection;
		private Material m_BorderRendererMaterial;
		private Transform m_IconTransform;
		private Material m_ButtonMaterial;
		private Vector3 m_VisibleInsetLocalScale;
		private Vector3 m_HiddenInsetLocalScale;
		private Vector3 m_HighlightedInsetLocalScale;
		private Vector3 m_OriginalIconLocalPosition;
		private Vector3 m_HiddenLocalScale;
		private Vector3 m_VisibleLocalScale;
		private Vector3 m_IconHighlightedLocalPosition;
		private Vector3 m_IconPressedLocalPosition;
		private Quaternion m_OriginalIconLocalRotation;
		private Vector3 m_OriginalForwardVector;
		private float m_IconLookForwardOffset = 0.5f;
		private Vector3 m_IconLookDirection;

		Coroutine m_VisibilityCoroutine;
		Coroutine m_ContentVisibilityCoroutine;

		// TODO DELETE THESE
		private Coroutine m_FadeInCoroutine;
		private Coroutine m_FadeOutCoroutine;
		private Coroutine m_HighlightCoroutine;
		private Coroutine m_IconHighlightCoroutine;
		private Coroutine m_IconEndHighlightCoroutine;

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

		private bool m_Pressed;
		public bool pressed
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

					if (m_IconEndHighlightCoroutine != null)
						StopCoroutine(m_IconEndHighlightCoroutine);

					if (m_IconHighlightCoroutine != null) // dont begin a new icon highlight coroutine; allow the currently running cortoutine to finish itself according to the m_Highlighted value
						StopCoroutine(m_IconHighlightCoroutine);

					m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow(true));
				}
			}
		}

		private bool m_Highlighted;
		public bool highlight
		{
			//get { return m_Highlighted; }
			set
			{
				if (m_Highlighted == value)
					return;
				else
				{
					if (m_IconEndHighlightCoroutine != null)
						StopCoroutine(m_IconEndHighlightCoroutine);

					m_Highlighted = value;
					Debug.LogError("<color=black>m_Highlighted set to : </color>" + m_Highlighted);
					if (m_Highlighted == true) // only start the highlight coroutine if the highlight coroutine isnt already playing. Otherwise allow it to gracefully finish.
					{
						if (m_HighlightCoroutine == null)
							m_HighlightCoroutine = StartCoroutine(Highlight());
					}
					else
						m_IconEndHighlightCoroutine = StartCoroutine(IconEndHighlight());
				}
			}
		}

		private static UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair sGradientPair;
		public UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair gradientPair
		{
			set
			{
				sGradientPair = value;
				m_BorderRendererMaterial.SetColor("_ColorTop", value.a);
				m_BorderRendererMaterial.SetColor("_ColorBottom", value.b);
			}
		}

		private void Awake()
		{
			m_ButtonMeshTransform = m_ButtonMeshRenderer.transform;
			m_ButtonMaterial = U.Material.GetMaterialClone(m_ButtonMeshRenderer);
			sOriginalInsetGradientPair = new Utilities.UnityBrandColorScheme.GradientPair (m_ButtonMaterial.GetColor("_ColorTop"), m_ButtonMaterial.GetColor("_ColorBottom"));
			m_HiddenLocalRotation = transform.localRotation;
			m_VisibleInsetLocalScale = m_ButtonMeshTransform.localScale;
			m_HighlightedInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_VisibleInsetLocalScale.y * 1.1f, m_VisibleInsetLocalScale.z);
			m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_ButtonMeshTransform.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
			m_VisibleLocalScale = transform.localScale;
			m_HiddenLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, 0f);
			m_HiddenInsetLocalScale = new Vector3(m_VisibleLocalScale.x, m_VisibleLocalScale.y, 0f);

			m_IconTransform = m_IconContainer;// m_Icon.transform;
			m_OriginalForwardVector = transform.forward;
			m_OriginalIconLocalPosition = m_IconTransform.localPosition;
			m_OriginalIconLocalRotation = m_IconTransform.localRotation;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * m_IconHighlightedLocalYOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * -m_IconHighlightedLocalYOffset;

			Show();

			//Debug.LogWarning("Icon original local rotation" + m_OriginalIconLocalRotation);
		}

		public void Show()
		{
			//m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale;
			m_Pressed = false;
			m_Highlighted = false;

			if (m_VisibilityCoroutine != null)
				StopCoroutine(m_VisibilityCoroutine);

			m_VisibilityCoroutine = StartCoroutine(AnimateShow());
		}

		public void Hide()
		{
			if (gameObject.activeInHierarchy)
			{
				if (m_FadeInCoroutine != null)
					StopCoroutine(m_FadeInCoroutine); // stop any fade in visuals

				if (m_FadeOutCoroutine == null)
					m_FadeOutCoroutine = StartCoroutine(AnimateHide()); // perform fade if not already performing
			}
		}

		private IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_ButtonMaterial.SetFloat("_Alpha", 0f);

			//m_ButtonMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			//m_ButtonMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);
			//m_BorderRendererMaterial.SetFloat("_Expand", 0);
			//m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale ;
			//m_IconTransform.localPosition = m_OriginalIconLocalPosition;

			if (m_ContentVisibilityCoroutine != null)
				StopCoroutine(m_ContentVisibilityCoroutine);

			m_ContentVisibilityCoroutine = StartCoroutine(ShowContent());

			const float kTargetDelay = 1f;
			float currentDelay = 0f;
			float shapedDelayLerp = 0f;
			Vector3 scale = m_HiddenLocalScale;
			Vector3 smoothVelocity = Vector3.zero;
			while (!Mathf.Approximately(scale.z, m_VisibleLocalScale.z)) // Z axis scales during the reveal
			{
				transform.localScale = scale;
				m_ButtonMaterial.SetFloat("_Alpha", scale.z);

				while (currentDelay < kTargetDelay)
				{
					currentDelay += Time.unscaledDeltaTime;
					transform.localScale = Vector3.Lerp(new Vector3(m_HiddenLocalScale.x, 0f, 0f), m_HiddenInsetLocalScale, shapedDelayLerp * shapedDelayLerp);
					shapedDelayLerp = currentDelay / kTargetDelay;
					yield return null;
				}

				scale = Vector3.SmoothDamp(scale, m_VisibleLocalScale, ref smoothVelocity, 0.5f, Mathf.Infinity, Time.unscaledDeltaTime);
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
			const float kTargetDelay = 2f;
			float currentDelay = 0f;
			float opacity = 0f;
			const float kTargetOpacity = 1f;
			float opacitySmoothVelocity = 1f;
			while (!Mathf.Approximately(m_CanvasGroup.alpha, 2f))
			{
				m_CanvasGroup.alpha = opacity;

				while (currentDelay < kTargetDelay)
				{
					currentDelay += Time.unscaledDeltaTime;
					yield return null;
				}

				opacity = Mathf.SmoothDamp(opacity, kTargetOpacity, ref opacitySmoothVelocity, 0.2f, Mathf.Infinity, Time.unscaledDeltaTime);
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
			m_CanvasGroup.interactable = true;
			m_ContentVisibilityCoroutine = null;
		}

		private IEnumerator AnimateHide()
		{
			m_CanvasGroup.interactable = false;
			m_Pressed = false;
			m_Highlighted = false;

			//if (m_HighlightCoroutine != null)
				//StopCoroutine(m_HighlightCoroutine);

			float opacity = m_ButtonMaterial.GetFloat("_Alpha");;
			float opacityShaped = Mathf.Pow(opacity, opacity);
			while (opacity > 0)
			{
				//if (orderIndex == 0)
				Vector3 newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
				transform.localScale = newScale;

				m_CanvasGroup.alpha = opacityShaped;
				m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
				m_ButtonMaterial.SetFloat("_Alpha", opacityShaped);
				m_ButtonMeshTransform.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacityShaped);
				opacity -= Time.unscaledDeltaTime * 1.5f;
				opacityShaped = Mathf.Pow(opacity, opacity);
				yield return null;
			}

			FadeOutCleanup();
			m_FadeOutCoroutine = null;
		}

		private void FadeOutCleanup()
		{
			m_CanvasGroup.alpha = 0;
			m_ButtonMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			m_ButtonMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat("_Expand", 1);
			m_ButtonMaterial.SetFloat("_Alpha", 0);
			m_ButtonMeshTransform.localScale = m_HiddenInsetLocalScale;
			transform.localScale = Vector3.zero;
		}

		private IEnumerator Highlight()
		{
			Debug.LogError("Starting Slot Highlight");
			
			if (m_IconHighlightCoroutine == null)
				m_IconHighlightCoroutine = StartCoroutine(IconHighlightAnimatedShow());

			float opacity = Time.unscaledDeltaTime;
			Color topColor = sOriginalInsetGradientPair.a;
			Color bottomColor = sOriginalInsetGradientPair.b;
			while (opacity > 0)
			{
				//Debug.Log(opacity + " - " + m_Highlighted.ToString());

				if (m_Highlighted)
				{
					if (!Mathf.Approximately(opacity, 1f))
						opacity = Mathf.Clamp01(opacity + Time.unscaledDeltaTime * 4); // stay highlighted
				}
				else
					opacity = Mathf.Clamp01(opacity - Time.unscaledDeltaTime * 2); //Mathf.PingPong(opacity + Time.unscaledDeltaTime * 4, 1); // ping pong out of the hide visual state if no longer highlighted

				//if (orderIndex == 0)
				//transform.localScale = new Vector3(opacity, 1f, 1f);

				//transform.localScale = Vector3.Lerp(hiddenScale, Vector3.one, opacity);
				//m_CanvasGroup.alpha = opacity;

				topColor = Color.Lerp(sOriginalInsetGradientPair.a, sGradientPair.a, opacity * 2f);
				bottomColor = Color.Lerp(sOriginalInsetGradientPair.b, sGradientPair.b, opacity);

				//m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
				m_ButtonMaterial.SetColor("_ColorTop", topColor);
				m_ButtonMaterial.SetColor("_ColorBottom", bottomColor);

				//m_ButtonMaterial.SetFloat("_Alpha", opacityShaped / 4);
				m_ButtonMeshTransform.localScale = Vector3.Lerp(m_VisibleInsetLocalScale, m_HighlightedInsetLocalScale, opacity * opacity);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_ButtonMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			m_ButtonMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);

			Debug.LogError("<color=green>Finished Slot Highlight</color>");

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
			Vector3 currentPosition = m_IconTransform.localPosition;
			Vector3 targetPosition = pressed == false ? m_IconHighlightedLocalPosition : m_IconPressedLocalPosition; // Raise up for highlight; lower for press
			float transitionAmount = Time.unscaledDeltaTime;
			float transitionAddMultiplier = pressed == false ? 14 : 18; // Faster transition in for standard highlight; slower for pressed highlight
			while (transitionAmount < 1)
			{
				Debug.LogError("Inside ICON HIGHLIGHT");
				m_IconTransform.localPosition = Vector3.Lerp(currentPosition, targetPosition, transitionAmount);
				transitionAmount = transitionAmount + Time.unscaledDeltaTime * transitionAddMultiplier;
				yield return null;
			}

			m_IconTransform.localPosition = targetPosition;
			m_IconHighlightCoroutine = null;
		}

		private IEnumerator IconEndHighlight()
		{
			Debug.LogError("<color=blue>ENDING ICON HIGHLIGHT</color> : " + m_IconTransform.localPosition);

			Vector3 currentPosition = m_IconTransform.localPosition;
			float transitionAmount = 1f; // this should account for the magnitude difference between the highlightedYPositionOffset, and the current magnitude difference between the local Y and the original Y
			float transitionSubtractMultiplier = 5f;//18;
			while (transitionAmount > 0)
			{
				m_IconTransform.localPosition = Vector3.Lerp(m_OriginalIconLocalPosition, currentPosition, transitionAmount);
				//Debug.LogError("transition amount : " + transitionAmount + "icon position : " + m_IconTransform.localPosition);
				transitionAmount -= Time.unscaledDeltaTime * transitionSubtractMultiplier;
				yield return null;
			}

			m_IconTransform.localPosition = m_OriginalIconLocalPosition;
			m_IconEndHighlightCoroutine = null;
		}
	}
}