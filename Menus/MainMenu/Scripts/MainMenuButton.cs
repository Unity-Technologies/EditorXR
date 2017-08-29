#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
	sealed class MainMenuButton : MonoBehaviour, ITooltip, IRayEnterHandler, IPointerClickHandler
	{
		[SerializeField]
		Button m_Button;

		[SerializeField]
		Text m_ButtonDescription;

		[SerializeField]
		Text m_ButtonTitle;

		Color m_OriginalColor;
		Transform m_InteractingRayOrigin;

		public Button button { get { return m_Button; } }

		public string tooltipText { get { return tooltip != null ? tooltip.tooltipText : null; } }

		public ITooltip tooltip { private get; set; }

		public void OnPointerClick(PointerEventData eventData)
		{
			if (clicked != null)
				clicked(m_InteractingRayOrigin);
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

		void OnDisable()
		{
			m_InteractingRayOrigin = null;
		}

		public void SetData(string name, string description)
		{
			m_ButtonTitle.text = name;
			m_ButtonDescription.text = description;
		}

		public void OnRayEnter(RayEventData eventData)
		{
			m_InteractingRayOrigin = eventData.rayOrigin;

			if (hovered != null)
				hovered(eventData.rayOrigin);
		}
	}
}
#endif
