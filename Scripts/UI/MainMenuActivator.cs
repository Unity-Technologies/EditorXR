using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.VR.Modules;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Menus
{
	internal class MainMenuActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IMenuOrigins
	{
		private readonly Vector3 m_OriginalActivatorLocalPosition = new Vector3(0f, 0f, -0.075f);
		private static readonly float kAlternateLocationOffset = 0.06f;

		public Transform alternateMenuOrigin
		{
			get { return m_AlternateMenuOrigin; }
			set
			{
				m_AlternateMenuOrigin = value;
				transform.SetParent(m_AlternateMenuOrigin);
				transform.localPosition = m_OriginalActivatorLocalPosition;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;

				m_OriginalActivatorIconLocalScale = m_Icon.localScale;
				m_OriginalActivatorIconLocalPosition = m_Icon.localPosition;
				m_HighlightedActivatorIconLocalScale = m_HighlightedPRS.localScale;
				m_HighlightedActivatorIconLocalPosition = m_HighlightedPRS.localPosition;
				m_AlternateActivatorLocalPosition = m_OriginalActivatorLocalPosition + Vector3.back * kAlternateLocationOffset;
			}
		}
		private Transform m_AlternateMenuOrigin;

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
		private bool m_ActivatorButtonMoveAway;

		[SerializeField]
		private Transform m_Icon;
		[SerializeField]
		private Transform m_HighlightedPRS;

		private Vector3 m_OriginalActivatorIconLocalScale;
		private Vector3 m_OriginalActivatorIconLocalPosition;
		private Vector3 m_HighlightedActivatorIconLocalScale;
		private Vector3 m_HighlightedActivatorIconLocalPosition;
		private Coroutine m_HighlightCoroutine;
		private Coroutine m_ActivatorMoveCoroutine;
		private Vector3 m_AlternateActivatorLocalPosition;

		public Node? node { private get; set; }
		public Transform menuOrigin { get; set; }

		public event Action<Transform> hoverStarted = delegate {};
		public event Action<Transform> hoverEnded = delegate {};
		public event Action<Node?> selected = delegate {};

		public void OnPointerEnter(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData != null)
				hoverStarted(rayEventData.rayOrigin);

			if (m_HighlightCoroutine != null)
				StopCoroutine(m_HighlightCoroutine);

			m_HighlightCoroutine = null;
			m_HighlightCoroutine = StartCoroutine(Highlight());
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (rayEventData != null)
				hoverEnded(rayEventData.rayOrigin);

			if (m_HighlightCoroutine != null)
				StopCoroutine(m_HighlightCoroutine);

			m_HighlightCoroutine = null;
			m_HighlightCoroutine = StartCoroutine(Highlight(false));
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			selected(node);
		}

		private IEnumerator Highlight(bool transitionIn = true)
		{
			var amount = 0f;
			var currentScale = m_Icon.localScale;
			var currentPosition = m_Icon.localPosition;
			var targetScale = transitionIn == true ? m_HighlightedActivatorIconLocalScale : m_OriginalActivatorIconLocalScale;
			var targetLocalPosition = transitionIn == true ? m_HighlightedActivatorIconLocalPosition : m_OriginalActivatorIconLocalPosition;
			var speed = (currentScale.x + 0.5f / targetScale.x) * 4; // perform faster is returning to original position

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
			var amount = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = moveAway ? m_AlternateActivatorLocalPosition : m_OriginalActivatorLocalPosition;
			var speed = (currentPosition.z / targetPosition.z) * (moveAway ? 10 : 3); // perform faster is returning to original position

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
