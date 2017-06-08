#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip, IPerformHaptics, IRayEnterHandler, IPointerClickHandler
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		Transform m_HoveringRayOrigin;
		Color m_OriginalColor;
		IPinnedToolButton m_HighlightedPinnedToolbutton;
		Transform m_RayOrigin;

		/// <summary>
		/// Highlights a pinned tool button when this menu button is highlighted
		/// </summary>
		public Func<Transform, Type, IPinnedToolButton> previewToolInPinnedToolButton { private get; set; }

		public string tooltipText { get; set; }

		public Type toolType { get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			this.Pulse(m_RayOrigin, 0.5f, 0.095f, true, true);
		}

		public bool selected
		{
			set
			{
				if (value)
				{
					m_Button.transition = Selectable.Transition.None;
					m_Button.targetGraphic.color = m_Button.colors.highlightedColor;
				}
				else
				{
					m_Button.transition = Selectable.Transition.ColorTint;
					m_Button.targetGraphic.color = m_OriginalColor;
				}
			}
		}

		private void Awake()
		{
			m_OriginalColor = m_Button.targetGraphic.color;
		}

		void OnDisable()
		{
			m_RayOrigin = null;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			// Track which pointer is over us, so this information can supply context (e.g. selecting a tool for a different hand)
			m_HoveringRayOrigin = eventData.rayOrigin;

			// Enable preview-mode on a pinned tool button; Display on the opposite proxy device via the HoveringRayOrigin
			m_HighlightedPinnedToolbutton = previewToolInPinnedToolButton(m_HoveringRayOrigin, toolType);

			m_RayOrigin = eventData.rayOrigin;
			this.Pulse(eventData.rayOrigin, 0.005f, 0.175f);
		}

		public void OnRayExit(RayEventData eventData)
		{
			if (m_HoveringRayOrigin == eventData.rayOrigin)
				m_HoveringRayOrigin = null;

			// Disable preview-mode on pinned tool button
			if (m_HighlightedPinnedToolbutton != null)
				m_HighlightedPinnedToolbutton.previewToolType = null;

			m_RayOrigin = null;
		}
	}
}
#endif
