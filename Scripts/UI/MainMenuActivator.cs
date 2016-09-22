using System;
using System.Collections;
using UnityEngine.EventSystems;

namespace UnityEngine.VR.Menus
{
	public class MainMenuActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IMenuOrigins
	{
		public Action onActivate;

		public Action onDeactivate;

		public void Activate()
		{

		}

		public void Deactivate()
		{

		}

		[SerializeField]
		private Transform m_Icon;
		[SerializeField]
		private Transform m_HighlightedPRS;

		private readonly Vector3 m_OriginalActivatorLocalPosition = new Vector3(0f, 0f, -0.075f);
		private static readonly float kAlternateLocationOffset = 0.175f;

		private Vector3 m_OriginalActivatorIconLocalScale;
		private Vector3 m_OriginalActivatorIconLocalPosition;
		private Vector3 m_HighlightedActivatorIconLocalScale;
		private Vector3 m_HighlightedActivatorIconLocalPosition;
		private Coroutine m_HighlightCoroutine;
		private Coroutine m_ActivatorMoveCoroutine;
		private Vector3 m_AlternateActivatorLocalPosition;

		public Transform menuOrigin { get; set; }

		private bool m_Activated;
		private bool activated
		{
			get { return m_Activated; }
			set
			{
				if (m_Activated == value || m_ActivatorMoveCoroutine != null) // prevent state change if the animation is still being performed
					return;

				m_Activated = value;

				if (m_Activated == true && onActivate != null)
					onActivate();

				if (m_Activated == false && onDeactivate != null)
					onDeactivate();

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(!m_Activated)); // Move back towards the original position if it activates the main menu
			}
		}

		private Transform m_AlternateMenuOrigin;
		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				Debug.LogError("<color=blue>Setting MAIN MENU ACTIVATOR position!</color>");
				transform.SetParent(m_AlternateMenuOrigin = value);
				transform.localPosition = m_OriginalActivatorLocalPosition;
				transform.localRotation = Quaternion.identity;

				m_OriginalActivatorIconLocalScale = m_Icon.localScale;
				m_OriginalActivatorIconLocalPosition = m_Icon.localPosition;
				m_HighlightedActivatorIconLocalScale = m_HighlightedPRS.localScale;
				m_HighlightedActivatorIconLocalPosition = m_HighlightedPRS.localPosition;
				m_AlternateActivatorLocalPosition = m_OriginalActivatorLocalPosition + Vector3.back * kAlternateLocationOffset;
			}
		}

		private bool m_ActivatorButtonMoveAway;
		public bool activatorButtonMoveAway
		{
			get { return m_ActivatorButtonMoveAway; }
			set
			{
				if (m_ActivatorButtonMoveAway == value)
					return;

				m_ActivatorButtonMoveAway = value;

				if (m_ActivatorMoveCoroutine != null)
					StopCoroutine(m_ActivatorMoveCoroutine);

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_ActivatorButtonMoveAway));
			}
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
			activated = !activated;
		}

		private IEnumerator Highlight(bool transitionIn = true)
		{
			float amount = 0f;
			Vector3 currentScale = m_Icon.localScale;
			Vector3 currentPosition = m_Icon.localPosition;
			Vector3 targetScale = transitionIn == true ? m_HighlightedActivatorIconLocalScale : m_OriginalActivatorIconLocalScale;
			Vector3 targetLocalPosition = transitionIn == true ? m_HighlightedActivatorIconLocalPosition : m_OriginalActivatorIconLocalPosition;
			float speed = (currentScale.x + 0.5f / targetScale.x) * 4; // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				m_Icon.localScale = Vector3.Lerp(currentScale, targetScale,  Mathf.SmoothStep(0f, 1f, amount));
				m_Icon.localPosition = Vector3.Lerp(currentPosition, targetLocalPosition,  Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			m_Icon.localScale = targetScale;
			m_Icon.localPosition = targetLocalPosition;
		}

		private IEnumerator AnimateMoveActivatorButton(bool moveAway = true)
		{
			Debug.LogError("Move Activator Button out of the way of the radial menu here");
			
			float amount = 0f;
			Vector3 currentPosition = transform.localPosition;
			Vector3 targetPosition = moveAway == true ? m_AlternateActivatorLocalPosition : m_OriginalActivatorLocalPosition;
			float speed = (currentPosition.z / targetPosition.z) * (moveAway ? 10 : 3); // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.unscaledDeltaTime * speed;
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			transform.localPosition = targetPosition;
			m_ActivatorMoveCoroutine = null;
		}
	}
}
