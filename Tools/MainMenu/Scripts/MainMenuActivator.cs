using System;
using System.Collections;
using UnityEngine.EventSystems;

namespace UnityEngine.VR.Menus
{
	public class MainMenuActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		[SerializeField]
		private Transform m_Icon;

		private Vector3 m_OriginalActivatorIconLocalScale;
		private Vector3 m_HighlightedActivatorIconLocalScale;
		private Coroutine m_HighlightCoroutine;

		/// <summary>
		/// Delegate assigned by menu, in order to open/close the menu
		/// </summary>
		public Action performActivation { get; set; }

		private void Awake()
		{
			m_OriginalActivatorIconLocalScale = m_Icon.localScale;
			m_HighlightedActivatorIconLocalScale = m_OriginalActivatorIconLocalScale * 2f;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerEnter called on MenuTrigger</color>");

			if (m_HighlightCoroutine != null)
				StopCoroutine(m_HighlightCoroutine);

			m_HighlightCoroutine = null;
			m_HighlightCoroutine = StartCoroutine(Highlight());
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerExit called on MenuTrigger</color>");
			if (m_HighlightCoroutine != null)
				StopCoroutine(m_HighlightCoroutine);

			m_HighlightCoroutine = null;
			m_HighlightCoroutine = StartCoroutine(Highlight(false));
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Debug.LogError("<color=green>OnPointerClick called on MenuTrigger</color>");
			Debug.LogError("Activate Menu");

			if (performActivation != null)
				performActivation();
		}

		private IEnumerator Highlight(bool transitionIn = true)
		{
			float amount = 0f;
			Vector3 currentScale = m_Icon.localScale;
			Vector3 targetScale = transitionIn == true ? m_HighlightedActivatorIconLocalScale : m_OriginalActivatorIconLocalScale;
			float speed = (currentScale.x / targetScale.x) * 4; // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				m_Icon.localScale = Vector3.Lerp(currentScale, targetScale,  Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			m_Icon.localScale = targetScale;
		}
	}
}