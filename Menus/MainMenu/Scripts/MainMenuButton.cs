#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip, IRayEnterHandler, IRayExitHandler, IRayClickHandler
	{
		[SerializeField]
		Button m_Button;

		[SerializeField]
		Text m_ButtonDescription;

		[SerializeField]
		Text m_ButtonTitle;

		Color m_OriginalColor;
		IPinnedToolButton m_HighlightedPinnedToolbutton;
		Transform m_RayOrigin;

		/// <summary>
		/// Highlights a pinned tool button when this menu button is highlighted
		/// </summary>
		public Func<Transform, Type, String, IPinnedToolButton> previewToolInPinnedToolButton { private get; set; }

		public Button button { get { return m_Button; } }

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : null; } }

		public ITooltip tooltip { private get; set; }

		public Type toolType { get; set; }

		public void OnRayClick(RayEventData eventData)
		{
			if (clicked != null)
				clicked(eventData.rayOrigin);
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

		public event Action<Transform> hovered;
		public event Action<Transform> clicked;

		void Awake()
		{
			m_OriginalColor = m_Button.targetGraphic.color;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			// Track which pointer is over us, so this information can supply context (e.g. selecting a tool for a different hand)
			var interactingRayOrigin = eventData.rayOrigin;
			if (toolType != null && interactingRayOrigin != null)
			{
				// Enable preview-mode on a pinned tool button; Display on the opposite proxy device via the HoveringRayOrigin
				m_HighlightedPinnedToolbutton = previewToolInPinnedToolButton(interactingRayOrigin, toolType, m_ButtonDescription.text);
				// TODO convert to a function that is returned, that is called if non-null, instead of a direct reference to the button.
			}

			//m_RayOrigin = eventData.rayOrigin; // TODO: evaluate this need for m_RayOrigin
			if (hovered != null)
				hovered(eventData.rayOrigin);
		}

		public void OnRayExit(RayEventData eventData)
		{
			// Disable preview-mode on pinned tool button
			if (m_HighlightedPinnedToolbutton != null)
				m_HighlightedPinnedToolbutton.previewToolType = null;
		
			if (hovered != null)
			hovered(eventData.rayOrigin);
		}
	}
}
#endif
