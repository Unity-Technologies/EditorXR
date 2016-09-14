using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

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

		private static Material sSharedInsetMaterialInstance;
		private static UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair sOriginalInsetGradientPair;
		
		private Transform m_parentTransform;
		private Vector3 m_IconDirection;
		private Material m_BorderRendererMaterial;
		private Transform m_IconTransform;
		private Material m_InsetMaterial;
		private Vector3 m_VisibleInsetLocalScale;
		private Vector3 m_HiddenInsetLocalScale;
		private Vector3 m_HighlightedInsetLocalScale;
		private Vector3 m_OriginalIconLocalPosition;
		private Vector3 m_HiddenLocalScale = new Vector3(1f, 0f, 1f);
		private Vector3 m_IconHighlightedLocalPosition;
		private Vector3 m_IconPressedLocalPosition;
		private Quaternion m_OriginalIconLocalRotation;
		private Vector3 m_OriginalForwardVector;
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
				m_BorderRendererMaterial = value; // TODO delete this reference if no longer needed
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
			m_InsetMaterial = U.Material.GetMaterialClone(m_InsetMeshRenderer);
			sOriginalInsetGradientPair = new Utilities.UnityBrandColorScheme.GradientPair (m_InsetMaterial.GetColor("_ColorTop"), m_InsetMaterial.GetColor("_ColorBottom"));
			m_HiddenLocalRotation = transform.localRotation;
			m_VisibleInsetLocalScale = m_MenuInset.localScale;
			m_HighlightedInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_VisibleInsetLocalScale.y * 1.1f, m_VisibleInsetLocalScale.z);
			m_VisibleInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, m_MenuInset.localScale.y * 0.35f, m_VisibleInsetLocalScale.z);
			m_HiddenInsetLocalScale = new Vector3(m_VisibleInsetLocalScale.x, 0f, m_VisibleInsetLocalScale.z);

			m_IconTransform = m_IconContainer;// m_Icon.transform;
			m_OriginalForwardVector = transform.forward;
			m_OriginalIconLocalPosition = m_IconTransform.localPosition;
			m_OriginalIconLocalRotation = m_IconTransform.localRotation;
			m_IconHighlightedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * m_IconHighlightedLocalYOffset;
			m_IconPressedLocalPosition = m_OriginalIconLocalPosition + Vector3.up * -m_IconHighlightedLocalYOffset;

			//Debug.LogWarning("Icon original local rotation" + m_OriginalIconLocalRotation);
		}

		public void Show()
		{
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			m_Pressed = false;
			m_Highlighted = false;

			if (m_FadeInCoroutine != null)
				StopCoroutine(m_FadeInCoroutine);

			if (m_FadeOutCoroutine != null)
				StopCoroutine(m_FadeOutCoroutine);

			m_FadeInCoroutine = StartCoroutine(AnimateShow());
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

		private void OnTransformParentChanged()
		{
			m_parentTransform = transform.parent;
			//m_IconDirection = m_parentTransform.local
			//Debug.LogError("<color=red>Radial Menu Slot Transform parent changed to : " + parentTransform.name + "</color>");
		}

		private void Update()
		{
			//m_IconTransform.localEulerAngles = new Vector3(m_IconTransform.localRotation.x, m_IconTransform.localRotation.y, m_IconTransform.localRotation.z);
			//m_Icon.transform.LookAt(m_parentTransform.forward);
			//m_IconTransform.localRotation = m_OriginalIconLocalRotation * m_parentTransform.localRotation;// Quaternion.Euler(m_OriginalIconLocalRotation.x, m_OriginalIconLocalRotation.y, m_OriginalIconLocalRotation.z);

			//iconLookDir = new Vector3 (m_IconTransform.localPosition.x, m_IconTransform.localPosition.y, m_IconTransform.localPosition.z + iconLookOffset);
			//lookDir = m_IconTransform.position + new Vector3(0f, 0f, (transform.parent.parent.forward * iconLookOffset).z);
			//iconLookDir = new Vector3(0f, lookDir.y, 0f);

			//iconLookDir = new Vector3(iconLookDir.x, m_IconTransform.position.y, iconLookDir.z);

			//conLookDir = m_Icon.transform.position +  transform.parent.forward * iconLookOffset; // set a position offset above the icon, regardless of the icon's rotation
																								  //m_IconTransform.LookAt(iconLookDir);

			//m_IconTransform.LookAt(new Vector3(iconLookDir.x, m_IconTransform.localPosition.y, iconLookDir.z));

			//m_IconTransform.LookAt(iconLookDir);
			

			//Quaternion relativeRotation = Quaternion.Inverse(m_IconTransform.localRotation) * m_OriginalIconLocalRotation;
			//m_IconTransform.localRotation = relativeRotation;// m_OriginalIconLocalRotation - m_parentTransform.localRotation - m_IconTransform.localRotation;
		}

		private void LateUpdate()
		{
			//m_IconTransform.localEulerAngles = new Vector3 (m_IconTransform.localRotation.x, m_IconTransform.localRotation.y, m_IconTransform.localRotation.z);

		}

		private void CorrectIconRotation()
		{
			m_IconLookDirection = m_Icon.transform.position + transform.parent.forward * m_IconLookForwardOffset; // set a position offset above the icon, regardless of the icon's rotation
			//m_IconTransform.LookAt(iconLookDir);
			//m_IconTransform.LookAt(new Vector3(iconLookDir.x, m_IconTransform.localPosition.y, iconLookDir.z));
			m_IconTransform.LookAt(m_IconLookDirection);
			m_IconTransform.localEulerAngles = new Vector3(0f, m_IconTransform.localEulerAngles.y, 0f);
		}

		/*
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(iconLookDir, 0.005f);
		}
		*/

		private IEnumerator AnimateShow()
		{
			m_CanvasGroup.interactable = false;
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_InsetMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_MenuInset.localScale = m_HiddenInsetLocalScale ;
			transform.localScale = m_HiddenLocalScale;
			m_IconTransform.localPosition = m_OriginalIconLocalPosition;
			
			StartCoroutine(ShowInset());
			
			float opacity = 0;
			float positionWait = orderIndex * 0.05f;
			while (opacity < 1)
			{
				//if (orderIndex == 0)
				//transform.localScale = new Vector3(opacity, 1f, 1f);
				opacity += Time.unscaledDeltaTime / positionWait;
				float opacityShaped = Mathf.Pow(opacity, opacity);

				transform.localScale = Vector3.Lerp(m_HiddenLocalScale, Vector3.one, opacity);
				//m_CanvasGroup.alpha = opacity * 0.25f;
				m_BorderRendererMaterial.SetFloat("_Expand", 1 - opacityShaped);
				//m_InsetMaterial.SetFloat("_Alpha", opacityShaped / 4);
				//m_MenuInset.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacity / 4);
				//m_CanvasGroup.alpha = opacity;
				CorrectIconRotation();
				yield return null;
			}


			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			//m_InsetMaterial.SetFloat("_Alpha", 1);
			//m_MenuInset.localScale = m_VisibleMenuInsetLocalScale;
			m_CanvasGroup.interactable = true;
			transform.localScale = Vector3.one;

			CorrectIconRotation();

			m_FadeInCoroutine = null;
		}

		private IEnumerator ShowInset()
		{
			m_CanvasGroup.alpha = 0.0001f;

			float opacity = 0f;
			float duration = 0f;
			float positionWait = orderIndex * 0.075f;
			while (duration < 2)
			{
				duration += Time.unscaledDeltaTime / positionWait;
				opacity = duration / 2;
				opacity *= opacity;
				//if (orderIndex == 0)
				m_CanvasGroup.alpha = Mathf.Clamp01(duration - 1);
				//transform.localScale = new Vector3(opacity, 1f, 1f);
				float opacityShaped = Mathf.Pow(opacity, opacity);
				m_InsetMaterial.SetFloat("_Alpha", opacity);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenInsetLocalScale, m_VisibleInsetLocalScale, opacity);
				yield return null;
			}

			//m_CanvasGroup.alpha = 1;
			m_InsetMaterial.SetFloat("_Alpha", 1);
			m_MenuInset.localScale = m_VisibleInsetLocalScale;
		}

		private IEnumerator AnimateHide()
		{
			m_CanvasGroup.interactable = false;
			m_Pressed = false;
			m_Highlighted = false;

			//if (m_HighlightCoroutine != null)
				//StopCoroutine(m_HighlightCoroutine);

			float opacity = m_InsetMaterial.GetFloat("_Alpha");;
			float opacityShaped = Mathf.Pow(opacity, opacity);
			while (opacity > 0)
			{
				//if (orderIndex == 0)
				Vector3 newScale = Vector3.one * opacity * opacityShaped * (opacity * 0.5f);
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
			m_InsetMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);
			m_BorderRendererMaterial.SetFloat("_Expand", 1);
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_MenuInset.localScale = m_HiddenInsetLocalScale;
			CorrectIconRotation();
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
				float opacityShaped = Mathf.Pow(opacity, opacity);

				//transform.localScale = Vector3.Lerp(hiddenScale, Vector3.one, opacity);
				//m_CanvasGroup.alpha = opacity;

				topColor = Color.Lerp(sOriginalInsetGradientPair.a, sGradientPair.a, opacity * 2f);
				bottomColor = Color.Lerp(sOriginalInsetGradientPair.b, sGradientPair.b, opacity);

				//m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
				m_InsetMaterial.SetColor("_ColorTop", topColor);
				m_InsetMaterial.SetColor("_ColorBottom", bottomColor);

				//m_InsetMaterial.SetFloat("_Alpha", opacityShaped / 4);
				m_MenuInset.localScale = Vector3.Lerp(m_VisibleInsetLocalScale, m_HighlightedInsetLocalScale, opacity * opacity);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}

			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_InsetMaterial.SetColor("_ColorTop", sOriginalInsetGradientPair.a);
			m_InsetMaterial.SetColor("_ColorBottom", sOriginalInsetGradientPair.b);

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