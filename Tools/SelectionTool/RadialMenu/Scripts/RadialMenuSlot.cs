using System.Collections;
using UnityEngine.UI;

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
		private Button m_Button;

		[SerializeField]
		private MeshRenderer m_BorderRenderer;

		private Transform m_parentTransform;
		private Vector3 m_IconDirection;
		private Material m_BorderRendererMaterial;

		private Material m_InsetMaterial;
		private Vector3 m_VisibleMenuInsetLocalScale;
		private Vector3 m_HiddenMenuInsetLocalScale;

		private Coroutine m_FadeInCoroutine;
		private Coroutine m_FadeOutCoroutine;

		public int orderIndex { get; set; }

		public Button button { get { return m_Button; } }

		private static Quaternion m_HiddenLocalRotation; // All menu slots share the same hidden location
		public static Quaternion hiddenLocalRotation { get { return m_HiddenLocalRotation; } }

		private Quaternion m_VisibleLocalRotation;
		public Quaternion visibleLocalRotation { get { return m_VisibleLocalRotation; } set { m_VisibleLocalRotation = value; } }

		private Sprite m_IconSprite;
		public Sprite iconSprite
		{
			set
			{
				m_IconSprite = value;
				m_Icon.sprite = m_IconSprite;
			}
		}

		public UnityEngine.VR.Utilities.UnityBrandColorScheme.GradientPair gradientPair
		{
			set
			{
				//m_InsetMaterial.SetColor("_ColorTop", value.a);
				//m_InsetMaterial.SetColor("_ColorBottom", value.b);
				m_BorderRendererMaterial.SetColor("_ColorTop", value.a);
				m_BorderRendererMaterial.SetColor("_ColorBottom", value.b);
			}
		}

		private void Awake()
		{
			m_HiddenLocalRotation = transform.localRotation;
			m_InsetMaterial = m_InsetMeshRenderer.sharedMaterial;
			m_VisibleMenuInsetLocalScale = m_MenuInset.localScale;
			m_HiddenMenuInsetLocalScale = new Vector3(m_VisibleMenuInsetLocalScale.x, 0f, m_VisibleMenuInsetLocalScale.z);
			m_BorderRendererMaterial = m_BorderRenderer.sharedMaterial;
		}

		private void OnEnable()
		{
			m_MenuInset.localScale = m_HiddenMenuInsetLocalScale;

			if (m_FadeInCoroutine != null)
				StopCoroutine(m_FadeInCoroutine);

			m_FadeInCoroutine = StartCoroutine(FadeSlotOpacityIn());
		}

		private void OnDisable()
		{
			if (gameObject.activeInHierarchy)
			{
				if (m_FadeOutCoroutine != null)
					StopCoroutine(m_FadeOutCoroutine);

				m_FadeOutCoroutine = StartCoroutine(FadeSlotOpacityOut());
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
			//m_Icon.transform.LookAt(m_parentTransform.forward);
		}

		private IEnumerator FadeSlotOpacityIn()
		{
			m_CanvasGroup.interactable = false;
			m_CanvasGroup.alpha = 0;
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_MenuInset.localScale = m_HiddenMenuInsetLocalScale ;
			transform.localScale = new Vector3(1f, 0f, 1f); //Vector3.one * 0.75f;
			Vector3 hiddenScale = transform.localScale;

			float opacity = 0;
			float positionWait = orderIndex * 0.05f;
			while (opacity < 1)
			{
				//if (orderIndex == 0)
				//transform.localScale = new Vector3(opacity, 1f, 1f);
				opacity += Time.unscaledDeltaTime / positionWait;
				float opacityShaped = Mathf.Pow(opacity, opacity);

				transform.localScale = Vector3.Lerp(hiddenScale, Vector3.one, opacity);
				m_CanvasGroup.alpha = opacityShaped;
				m_BorderRendererMaterial.SetFloat("_Expand", 1 - opacityShaped);
				m_InsetMaterial.SetFloat("_Alpha", opacityShaped);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacityShaped);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}

			m_CanvasGroup.alpha = 1;
			m_BorderRendererMaterial.SetFloat("_Expand", 0);
			m_InsetMaterial.SetFloat("_Alpha", 1);
			m_MenuInset.localScale = m_VisibleMenuInsetLocalScale;
			m_CanvasGroup.interactable = true;
			transform.localScale = Vector3.one;

			m_FadeInCoroutine = null;
		}

		private IEnumerator FadeSlotOpacityOut()
		{
			m_CanvasGroup.interactable = false;

			float opacity = m_InsetMaterial.GetFloat("_Alpha");;
			while (opacity > 0)
			{
				opacity -= Time.unscaledDeltaTime * 1.5f;
				float opacityShaped = Mathf.Pow(opacity, opacity);
				//if (orderIndex == 0)
					transform.localScale = Vector3.one * opacity * opacityShaped;

				m_CanvasGroup.alpha = opacityShaped;
				m_BorderRendererMaterial.SetFloat("_Expand", opacityShaped);
				m_InsetMaterial.SetFloat("_Alpha", opacityShaped);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacityShaped);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}

			m_CanvasGroup.alpha = 0;
			m_BorderRendererMaterial.SetFloat("_Expand", 1);
			m_InsetMaterial.SetFloat("_Alpha", 0);
			m_MenuInset.localScale = m_HiddenMenuInsetLocalScale;
			transform.localScale = Vector3.zero;

			m_FadeOutCoroutine = null;
		}
	}
}