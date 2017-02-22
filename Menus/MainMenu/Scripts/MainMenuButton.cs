using System;
using UnityEngine.UI;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	internal sealed class MainMenuButton : MonoBehaviour, IRayEnterHandler, IRayExitHandler
	{
		public Button button { get { return m_Button; } }
		[SerializeField]
		private Button m_Button;

		[SerializeField]
		private Text m_ButtonDescription;
		[SerializeField]
		private Text m_ButtonTitle;

		Color m_OriginalColor;

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

		/// <summary>
		/// The ray that is hovering over the button
		/// </summary>
		public Transform hoveringRayOrigin { get; private set; }

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
			hoveringRayOrigin = eventData.rayOrigin;
		}

		public void OnRayExit(RayEventData eventData)
		{
			if (hoveringRayOrigin == eventData.rayOrigin)
				hoveringRayOrigin = null;
		}
	}
}