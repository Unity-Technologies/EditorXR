#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuActivator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
		IUsesMenuOrigins, IControlHaptics, IUsesHandedRayOrigin, ITooltip, ITooltipPlacement
	{
		readonly Vector3 m_OriginalActivatorLocalPosition = new Vector3(0f, 0f, -0.075f);
		static readonly float k_AlternateLocationOffset = 0.06f;

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

				var iconTransform = m_Icon.transform;
				m_OriginalActivatorIconLocalScale = iconTransform.localScale;
				m_OriginalActivatorIconLocalPosition = iconTransform.localPosition;
				m_HighlightedActivatorIconLocalScale = m_HighlightedPRS.localScale;
				m_HighlightedActivatorIconLocalPosition = m_HighlightedPRS.localPosition;
				m_AlternateActivatorLocalPosition = m_OriginalActivatorLocalPosition + Vector3.back * k_AlternateLocationOffset;
			}
		}
		Transform m_AlternateMenuOrigin;

		public bool activatorButtonMoveAway
		{
			get { return m_ActivatorButtonMoveAway; }
			set
			{
				if (m_ActivatorButtonMoveAway == value)
					return;

				m_ActivatorButtonMoveAway = value;

				this.StopCoroutine(ref m_ActivatorMoveCoroutine);

				m_ActivatorMoveCoroutine = StartCoroutine(AnimateMoveActivatorButton(m_ActivatorButtonMoveAway));
			}
		}
		bool m_ActivatorButtonMoveAway;

		[SerializeField]
		Renderer m_Icon;

		[SerializeField]
		Transform m_HighlightedPRS;

		[SerializeField]
		HapticPulse m_HoverPulse;

		[SerializeField]
		Color m_DisabledColor;

		[SerializeField]
		Transform m_TooltipSource;

		[SerializeField]
		Transform m_TooltipTarget;

		Vector3 m_OriginalActivatorIconLocalScale;
		Vector3 m_OriginalActivatorIconLocalPosition;
		Vector3 m_HighlightedActivatorIconLocalScale;
		Vector3 m_HighlightedActivatorIconLocalPosition;
		Coroutine m_HighlightCoroutine;
		Coroutine m_ActivatorMoveCoroutine;
		Vector3 m_AlternateActivatorLocalPosition;

		bool m_Disabled;
		Material m_IconMaterial;
		Color m_EnabledColor;

		public Transform rayOrigin { private get; set; }
		public Transform menuOrigin { private get; set; }
		public Node? node { get; set; }

		public event Action<Transform, Transform> selected;

		public bool disabled
		{
			get { return m_Disabled; }
			set
			{
				if (value != m_Disabled)
				{
					m_Icon.sharedMaterial.color = value ? m_DisabledColor : m_EnabledColor;

					if (value)
						SetHighlight(false);
				}

				m_Disabled = value;
			}
		}

		public string tooltipText
		{
			get { return m_Disabled ? "Main Menu Hidden" : null; }
		}

		public Transform tooltipTarget { get { return m_TooltipTarget; } }
		public Transform tooltipSource { get { return m_TooltipSource; } }
		public TextAlignment tooltipAlignment { get { return TextAlignment.Center; } }

		void Awake()
		{
			m_IconMaterial = MaterialUtils.GetMaterialClone(m_Icon);
			m_EnabledColor = m_IconMaterial.color;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (eventData.used || m_Disabled)
				return;

			SetHighlight(true);
			this.Pulse(node, m_HoverPulse);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (eventData.used)
				return;

			SetHighlight(false);
		}

		void SetHighlight(bool highlighted)
		{
			if (m_HighlightCoroutine != null)
				StopCoroutine(m_HighlightCoroutine);

			m_HighlightCoroutine = null;
			m_HighlightCoroutine = StartCoroutine(Highlight(highlighted));
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			var rayEventData = eventData as RayEventData;
			if (selected != null)
				selected(rayOrigin, rayEventData != null ? rayEventData.rayOrigin : null);
		}

		IEnumerator Highlight(bool transitionIn = true)
		{
			var amount = 0f;
			var iconTransform = m_Icon.transform;
			var currentScale = iconTransform.localScale;
			var currentPosition = iconTransform.localPosition;
			var targetScale = transitionIn == true ? m_HighlightedActivatorIconLocalScale : m_OriginalActivatorIconLocalScale;
			var targetLocalPosition = transitionIn == true ? m_HighlightedActivatorIconLocalPosition : m_OriginalActivatorIconLocalPosition;
			var speed = (currentScale.x + 0.5f / targetScale.x) * 4; // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.deltaTime * speed;
				iconTransform.localScale = Vector3.Lerp(currentScale, targetScale,  Mathf.SmoothStep(0f, 1f, amount));
				iconTransform.localPosition = Vector3.Lerp(currentPosition, targetLocalPosition,  Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			iconTransform.localScale = targetScale;
			iconTransform.localPosition = targetLocalPosition;
		}

		IEnumerator AnimateMoveActivatorButton(bool moveAway = true)
		{
			var amount = 0f;
			var currentPosition = transform.localPosition;
			var targetPosition = moveAway ? m_AlternateActivatorLocalPosition : m_OriginalActivatorLocalPosition;
			var speed = (currentPosition.z / targetPosition.z) * (moveAway ? 10 : 3); // perform faster is returning to original position

			while (amount < 1f)
			{
				amount += Time.deltaTime * speed;
				transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.SmoothStep(0f, 1f, amount));
				yield return null;
			}

			transform.localPosition = targetPosition;
			m_ActivatorMoveCoroutine = null;
		}
	}
}
#endif
