#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
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

		/// <summary>
		/// Highlights a pinned tool button when this menu button is highlighted
		/// </summary>
		public Func<Transform, Type, IPinnedToolButton> previewToolInPinnedToolButton { private get; set; }

		public Type toolType { get; set; }

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

				// HACK: Force update of target graphic color
				m_Button.enabled = false;
				m_Button.enabled = true;
			}
		}

		private void Awake()
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
			m_HoveringRayOrigin = eventData.rayOrigin;

			// Enable preview-mode on a pinned tool button; Display on the opposite proxy device via the HoveringRayOrigin
			m_HighlightedPinnedToolbutton = previewToolInPinnedToolButton(m_HoveringRayOrigin, toolType);
		}

		public void OnRayExit(RayEventData eventData)
		{
			if (m_HoveringRayOrigin == eventData.rayOrigin)
				m_HoveringRayOrigin = null;

			// Disable preview-mode on pinned tool button
			if (m_HighlightedPinnedToolbutton != null)
				m_HighlightedPinnedToolbutton.previewToolType = null;
		}
	}
}
#endif
