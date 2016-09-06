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

		private Transform m_parentTransform;
		private Vector3 m_IconDirection;

		private Material m_Material;
		private Vector3 m_VisibleMenuInsetLocalScale;
		private Vector3 m_HiddenMenuInsetLocalScale;

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

		private void Awake()
		{
			m_HiddenLocalRotation = transform.localRotation;
			m_Material = m_InsetMeshRenderer.sharedMaterial;
			m_VisibleMenuInsetLocalScale = m_MenuInset.localScale;
			m_HiddenMenuInsetLocalScale = new Vector3(m_VisibleMenuInsetLocalScale.x, 0f, m_VisibleMenuInsetLocalScale.z);
		}

		private void OnEnable()
		{
			m_MenuInset.localScale = m_HiddenMenuInsetLocalScale;
			StartCoroutine(FadeSlotOpacityIn());
		}

		private void OnDisable()
		{
			//StartCoroutine(FadeSlotOpacityOut());
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
			m_Material.SetFloat("_Alpha", 0);
			m_MenuInset.localScale = m_HiddenMenuInsetLocalScale;

			float opacity = 0;
			while (opacity < 1)
			{
				opacity += Time.unscaledDeltaTime / 2f;
				m_Material.SetFloat("_Alpha", opacity);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacity);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}

			m_CanvasGroup.interactable = true;
		}

		private IEnumerator FadeSlotOpacityOut()
		{
			m_CanvasGroup.interactable = false;

			float opacity = m_Material.GetFloat("_Alpha");
			while (opacity > 0)
			{
				opacity -= Time.unscaledDeltaTime * 1.5f;
				m_Material.SetFloat("_Alpha", opacity);
				m_MenuInset.localScale = Vector3.Lerp(m_HiddenMenuInsetLocalScale, m_VisibleMenuInsetLocalScale, opacity * opacity);
				//m_CanvasGroup.alpha = opacity;
				yield return null;
			}
		}
	}
}